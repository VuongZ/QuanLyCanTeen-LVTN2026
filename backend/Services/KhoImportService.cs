using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Services
{
    public class KhoImportService
    {
        private readonly AppDbContext _context;

        public KhoImportService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<int> CreateImportTicketAsync(CreateImportTicketDto dto)
        {
            if (dto.ManagerId <= 0)
                throw new InvalidOperationException("Không tìm thấy thông tin quản lý.");

            if (dto.BranchId <= 0)
                throw new InvalidOperationException("Không tìm thấy thông tin chi nhánh.");

            if (dto.SupplierId <= 0)
                throw new InvalidOperationException("Vui lòng chọn nhà cung cấp.");

            if (dto.Items == null || dto.Items.Count == 0)
                throw new InvalidOperationException("Phiếu nhập không có sản phẩm hợp lệ.");

            var manager = await _context.NsUsers
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == dto.ManagerId);

            if (manager == null)
                throw new InvalidOperationException("Không tìm thấy tài khoản quản lý.");

            if (manager.BranchId != dto.BranchId)
                throw new InvalidOperationException("Quản lý không thuộc chi nhánh nhập kho.");

            var branchExists = await _context.DmBranches.AnyAsync(b => b.Id == dto.BranchId);
            if (!branchExists)
                throw new InvalidOperationException("Không tìm thấy chi nhánh.");

            var supplierExists = await _context.KhoSuppliers.AnyAsync(s => s.Id == dto.SupplierId);
            if (!supplierExists)
                throw new InvalidOperationException("Không tìm thấy nhà cung cấp.");

            var invoiceCode = string.IsNullOrWhiteSpace(dto.InvoiceCode)
                ? null
                : dto.InvoiceCode.Trim();

            if (!string.IsNullOrWhiteSpace(invoiceCode))
            {
                var duplicatedInvoice = await _context.KhoImportTickets.AnyAsync(t =>
                    t.SupplierId == dto.SupplierId &&
                    t.InvoiceCode == invoiceCode);

                if (duplicatedInvoice)
                    throw new InvalidOperationException("Mã hóa đơn này đã được nhập cho nhà cung cấp đã chọn.");
            }

            var validItems = dto.Items
                .Where(i => !string.IsNullOrWhiteSpace(i.ProductName) || !string.IsNullOrWhiteSpace(i.ProductCode) || i.ProductId > 0)
                .ToList();

            if (validItems.Count == 0)
                throw new InvalidOperationException("Phiếu nhập không có sản phẩm hợp lệ.");

            foreach (var item in validItems)
            {
                if (item.Quantity <= 0)
                    throw new InvalidOperationException($"Số lượng của sản phẩm '{item.ProductName}' phải lớn hơn 0.");

                if (item.UnitPrice < 0)
                    throw new InvalidOperationException($"Đơn giá của sản phẩm '{item.ProductName}' không được âm.");
            }

            var totalAmount = validItems.Sum(i => i.Quantity * i.UnitPrice);

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var ticket = new KhoImportTicket
                {
                    ManagerId = dto.ManagerId,
                    BranchId = dto.BranchId,
                    SupplierId = dto.SupplierId,
                    InvoiceCode = invoiceCode,
                    InvoiceDate = dto.InvoiceDate.HasValue
                        ? DateOnly.FromDateTime(dto.InvoiceDate.Value)
                        : null,
                    ImportDate = DateTime.Now,
                    TotalAmount = totalAmount,
                    Note = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim()
                };

                _context.KhoImportTickets.Add(ticket);
                await _context.SaveChangesAsync();

                foreach (var item in validItems)
                {
                    var product = await FindOrCreateProductAsync(item, dto.SupplierId);

                    var unitAtTime = !string.IsNullOrWhiteSpace(item.Unit)
                        ? item.Unit.Trim()
                        : product.Unit;

                    var detail = new KhoImportDetail
                    {
                        ImportId = ticket.Id,
                        ProductId = product.Id,
                        UnitAtTime = unitAtTime,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice
                    };

                    _context.KhoImportDetails.Add(detail);

                    var inventory = await _context.KhoBranchInventories
                        .FirstOrDefaultAsync(i =>
                            i.BranchId == dto.BranchId &&
                            i.ProductId == product.Id);

                    if (inventory == null)
                    {
                        inventory = new KhoBranchInventory
                        {
                            BranchId = dto.BranchId,
                            ProductId = product.Id,
                            Quantity = item.Quantity
                        };

                        _context.KhoBranchInventories.Add(inventory);
                    }
                    else
                    {
                        inventory.Quantity += item.Quantity;
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return ticket.Id;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

   public async Task<List<InventoryImportTicketListDto>> GetInventoryImportTicketsAsync(int? branchId)
{
    var query = _context.KhoImportTickets
        .AsNoTracking()
        .Include(t => t.Branch)
        .Include(t => t.Manager)
        .Include(t => t.Supplier)
        .Include(t => t.KhoImportDetails)
        .AsQueryable();

    if (branchId.HasValue && branchId.Value > 0)
    {
        query = query.Where(t => t.BranchId == branchId.Value);
    }

    var tickets = await query
        .OrderByDescending(t => t.Id)
        .ToListAsync();

    return tickets.Select(t => new InventoryImportTicketListDto
    {
        Id = t.Id,
        BranchId = t.BranchId,
        BranchName = t.Branch?.Name ?? "Chưa rõ cơ sở",
        ManagerName = t.Manager?.FullName ?? "Chưa rõ người nhập",
        SupplierName = t.Supplier?.SupplierName ?? "Chưa rõ NCC",
        InvoiceCode = t.InvoiceCode,
        InvoiceDate = FormatDate(t.InvoiceDate),
        ImportDate = FormatDateTime(t.ImportDate),
        TotalAmount = Convert.ToDecimal(t.TotalAmount),
        TotalQuantity = t.KhoImportDetails.Sum(d => Convert.ToInt32(d.Quantity)),
        ItemCount = t.KhoImportDetails.Count,
        Note = t.Note
    }).ToList();
}

public async Task<InventoryImportTicketDetailDto?> GetInventoryImportTicketDetailAsync(int id, int? branchId)
{
    var query = _context.KhoImportTickets
        .AsNoTracking()
        .Include(t => t.Branch)
        .Include(t => t.Manager)
        .Include(t => t.Supplier)
        .Include(t => t.KhoImportDetails)
            .ThenInclude(d => d.Product)
        .AsQueryable();

    if (branchId.HasValue && branchId.Value > 0)
    {
        query = query.Where(t => t.BranchId == branchId.Value);
    }

    var ticket = await query.FirstOrDefaultAsync(t => t.Id == id);

    if (ticket == null) return null;

    return new InventoryImportTicketDetailDto
    {
        Id = ticket.Id,
        BranchId = ticket.BranchId,
        BranchName = ticket.Branch?.Name ?? "Chưa rõ cơ sở",
        ManagerName = ticket.Manager?.FullName ?? "Chưa rõ người nhập",
        SupplierName = ticket.Supplier?.SupplierName ?? "Chưa rõ NCC",
        InvoiceCode = ticket.InvoiceCode,
        InvoiceDate = FormatDate(ticket.InvoiceDate),
        ImportDate = FormatDateTime(ticket.ImportDate),
        TotalAmount = Convert.ToDecimal(ticket.TotalAmount),
        TotalQuantity = ticket.KhoImportDetails.Sum(d => Convert.ToInt32(d.Quantity)),
        ItemCount = ticket.KhoImportDetails.Count,
        Note = ticket.Note,
        Items = ticket.KhoImportDetails.Select(d => new InventoryImportTicketItemDto
        {
            ProductId = d.ProductId,
            ProductCode = d.Product?.ProductCode,
            ProductName = d.Product?.ProductName ?? "Chưa rõ sản phẩm",
            Unit = d.UnitAtTime ?? d.Product?.Unit,
            Quantity = Convert.ToInt32(d.Quantity),
            UnitPrice = Convert.ToDecimal(d.UnitPrice),
            LineTotal = Convert.ToInt32(d.Quantity) * Convert.ToDecimal(d.UnitPrice)
        }).ToList()
    };
}

private static string FormatDateTime(object? value)
{
    if (value == null) return "";

    if (value is DateTime dateTime)
        return dateTime.ToString("dd/MM/yyyy HH:mm");

    if (DateTime.TryParse(value.ToString(), out var parsed))
        return parsed.ToString("dd/MM/yyyy HH:mm");

    return value.ToString() ?? "";
}

private static string? FormatDate(object? value)
{
    if (value == null) return null;

    if (value is DateOnly dateOnly)
        return dateOnly.ToString("dd/MM/yyyy");

    if (value is DateTime dateTime)
        return dateTime.ToString("dd/MM/yyyy");

    if (DateTime.TryParse(value.ToString(), out var parsed))
        return parsed.ToString("dd/MM/yyyy");

    return value.ToString();
}

        private async Task<KhoProduct> FindOrCreateProductAsync(ImportItemDto item, int supplierId)
        {
            var productName = item.ProductName?.Trim();
            var productCode = item.ProductCode?.Trim();
            var unit = string.IsNullOrWhiteSpace(item.Unit) ? "Cái" : item.Unit.Trim();

            if (item.ProductId > 0)
            {
                var productById = await _context.KhoProducts
                    .FirstOrDefaultAsync(p => p.Id == item.ProductId);

                if (productById != null)
                    return productById;
            }

            if (!string.IsNullOrWhiteSpace(productCode))
            {
                var productByCode = await _context.KhoProducts
                    .FirstOrDefaultAsync(p => p.ProductCode == productCode);

                if (productByCode != null)
                    return productByCode;
            }

            if (!string.IsNullOrWhiteSpace(productName))
            {
                var lowerName = productName.ToLower();

                var productByName = await _context.KhoProducts
                    .FirstOrDefaultAsync(p => p.ProductName.ToLower() == lowerName);

                if (productByName != null)
                {
                    if (string.IsNullOrWhiteSpace(productByName.ProductCode) && !string.IsNullOrWhiteSpace(productCode))
                    {
                        productByName.ProductCode = productCode;
                    }

                    if (string.IsNullOrWhiteSpace(productByName.Unit) && !string.IsNullOrWhiteSpace(unit))
                    {
                        productByName.Unit = unit;
                    }

                    return productByName;
                }
            }

            if (string.IsNullOrWhiteSpace(productName))
                throw new InvalidOperationException("Sản phẩm mới phải có tên sản phẩm.");

            var newProduct = new KhoProduct
            {
                ProductCode = string.IsNullOrWhiteSpace(productCode) ? null : productCode,
                ProductName = productName,
                Unit = unit,
                SupplierId = supplierId
            };

            _context.KhoProducts.Add(newProduct);
            await _context.SaveChangesAsync();

            return newProduct;
        }

        
    }
}
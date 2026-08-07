using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Repositories
{
    /// <summary>
    /// Thực hiện các thao tác Database
    /// liên quan đến nghiệp vụ nhập kho.
    ///
    /// Repository chỉ chịu trách nhiệm:
    /// - Truy vấn dữ liệu.
    /// - Thêm và cập nhật Entity.
    /// - Lưu thay đổi.
    /// - Quản lý transaction.
    /// </summary>
    public class KhoImportRepo
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// Nhận AppDbContext thông qua
        /// Dependency Injection.
        /// </summary>
        public KhoImportRepo(
            AppDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // KIỂM TRA NGƯỜI DÙNG, CHI NHÁNH VÀ NHÀ PHÂN PHỐI
        // =====================================================

        /// <summary>
        /// Tìm tài khoản người thực hiện nhập kho theo ID.
        ///
        /// Không lấy những tài khoản đã bị xóa mềm.
        /// </summary>
        public async Task<NsUser?> GetUserByIdAsync(
            int userId)
        {
            return await _context.NsUsers
                .Include(user => user.Role)
                .FirstOrDefaultAsync(user =>
                    user.Id == userId &&
                    user.IsDeleted != true
                );
        }

        /// <summary>
        /// Kiểm tra chi nhánh có tồn tại hay không.
        /// </summary>
        public async Task<bool> BranchExistsAsync(
            int branchId)
        {
            return await _context.DmBranches
                .AnyAsync(branch =>
                    branch.Id == branchId
                );
        }

        public async Task<bool> BranchIsActiveAsync(
            int branchId)
        {
            return await _context.DmBranches
                .AsNoTracking()
                .AnyAsync(branch =>
                    branch.Id == branchId &&
                    branch.IsActive
                );
        }

        /// <summary>
        /// Kiểm tra nhà phân phối có tồn tại
        /// và chưa bị xóa mềm hay không.
        /// </summary>
        public async Task<bool> SupplierExistsAsync(
            int supplierId)
        {
            return await _context.KhoSuppliers
                .AnyAsync(supplier =>
                    supplier.Id == supplierId &&
                    supplier.IsDeleted != true
                );
        }

        /// <summary>
        /// Kiểm tra mã hóa đơn đã được nhập
        /// cho nhà phân phối hay chưa.
        /// </summary>
        public async Task<bool> ImportInvoiceExistsAsync(
            int supplierId,
            string invoiceCode)
        {
            return await _context.KhoImportTickets
                .AnyAsync(ticket =>
                    ticket.SupplierId == supplierId &&
                    ticket.InvoiceCode == invoiceCode
                );
        }

        // =====================================================
        // TÌM VÀ TẠO SẢN PHẨM
        // =====================================================

        /// <summary>
        /// Tìm sản phẩm theo ID.
        /// </summary>
        public async Task<KhoProduct?>
            GetProductByIdAsync(int productId)
        {
            return await _context.KhoProducts
                .FirstOrDefaultAsync(product =>
                    product.Id == productId
                );
        }

        /// <summary>
        /// Tìm sản phẩm theo mã sản phẩm.
        /// </summary>
        public async Task<KhoProduct?>
            GetProductByCodeAsync(
                string productCode)
        {
            return await _context.KhoProducts
                .FirstOrDefaultAsync(product =>
                    product.ProductCode ==
                    productCode
                );
        }

        /// <summary>
        /// Tìm sản phẩm theo tên sản phẩm.
        ///
        /// Database đang sử dụng collation
        /// không phân biệt chữ hoa và chữ thường.
        /// </summary>
        public async Task<KhoProduct?>
            GetProductByNameAsync(
                string productName)
        {
            return await _context.KhoProducts
                .FirstOrDefaultAsync(product =>
                    product.ProductName ==
                    productName
                );
        }

        /// <summary>
        /// Thêm sản phẩm mới và lưu ngay
        /// để nhận được ProductId.
        /// </summary>
        public async Task<KhoProduct>
            AddProductAsync(
                KhoProduct product)
        {
            _context.KhoProducts.Add(product);

            await _context.SaveChangesAsync();

            return product;
        }

        public async Task<List<ProductAdminDto>>
            GetProductsForAdminAsync(bool active)
        {
            return await _context.KhoProducts
                .AsNoTracking()
                .Include(product => product.Supplier)
                .Where(product =>
                    (product.IsActive ?? true) == active)
                .OrderBy(product => product.ProductName)
                .Select(product => new ProductAdminDto
                {
                    Id = product.Id,
                    ProductCode = product.ProductCode,
                    ProductName = product.ProductName,
                    Unit = product.Unit,
                    SupplierId = product.SupplierId,
                    SupplierName = product.Supplier != null
                        ? product.Supplier.SupplierName
                        : null,
                    IsActive = product.IsActive ?? true,
                    InactiveAt = product.InactiveAt,
                    InactiveBy = product.InactiveBy,
                    InactiveReason = product.InactiveReason,
                    TotalInventory = product.KhoBranchInventories
                        .Sum(item => item.Quantity ?? 0),
                    TotalFrontStock = product.KhoBranchFrontStocks
                        .Sum(item => item.Quantity ?? 0)
                })
                .ToListAsync();
        }

        public async Task<KhoProduct?>
            GetProductForStatusChangeAsync(int productId)
        {
            return await _context.KhoProducts
                .FirstOrDefaultAsync(product =>
                    product.Id == productId);
        }

        public async Task<(int Inventory, int FrontStock)>
            GetProductStockTotalsAsync(int productId)
        {
            var inventory = await _context.KhoBranchInventories
                .Where(item => item.ProductId == productId)
                .SumAsync(item => (int?)(item.Quantity ?? 0)) ?? 0;

            var frontStock = await _context.KhoBranchFrontStocks
                .Where(item => item.ProductId == productId)
                .SumAsync(item => (int?)(item.Quantity ?? 0)) ?? 0;

            return (inventory, frontStock);
        }

        // =====================================================
        // PHIẾU NHẬP VÀ CHI TIẾT PHIẾU
        // =====================================================

        /// <summary>
        /// Thêm phiếu nhập và lưu ngay
        /// để nhận được mã phiếu nhập.
        /// </summary>
        public async Task<KhoImportTicket>
            AddImportTicketAsync(
                KhoImportTicket ticket)
        {
            _context.KhoImportTickets.Add(
                ticket
            );

            await _context.SaveChangesAsync();

            return ticket;
        }

        /// <summary>
        /// Thêm một dòng chi tiết phiếu nhập.
        ///
        /// Phương thức này chưa gọi SaveChanges
        /// để có thể lưu nhiều dòng cùng lúc.
        /// </summary>
        public void AddImportDetail(
            KhoImportDetail detail)
        {
            _context.KhoImportDetails.Add(
                detail
            );
        }

        // =====================================================
        // TỒN KHO CHI NHÁNH
        // =====================================================

        /// <summary>
        /// Tìm tồn kho của một sản phẩm
        /// tại một chi nhánh.
        ///
        /// Kiểm tra cả những Entity vừa được thêm
        /// nhưng chưa SaveChanges để tránh tạo trùng
        /// khi một sản phẩm xuất hiện nhiều lần
        /// trong cùng phiếu nhập.
        /// </summary>
        public async Task<KhoBranchInventory?>
            GetBranchInventoryAsync(
                int branchId,
                int productId)
        {
            var trackedInventory =
                _context.KhoBranchInventories
                    .Local
                    .FirstOrDefault(inventory =>
                        inventory.BranchId ==
                            branchId &&
                        inventory.ProductId ==
                            productId
                    );

            if (trackedInventory != null)
            {
                return trackedInventory;
            }

            return await _context
                .KhoBranchInventories
                .FirstOrDefaultAsync(inventory =>
                    inventory.BranchId ==
                        branchId &&
                    inventory.ProductId ==
                        productId
                );
        }

        /// <summary>
        /// Thêm một dòng tồn kho mới
        /// cho chi nhánh.
        ///
        /// Phương thức này chưa gọi SaveChanges.
        /// </summary>
        public void AddBranchInventory(
            KhoBranchInventory inventory)
        {
            _context.KhoBranchInventories.Add(
                inventory
            );
        }

        // =====================================================
        // LỊCH SỬ PHIẾU NHẬP
        // =====================================================

        /// <summary>
        /// Lấy danh sách lịch sử phiếu nhập kho.
        ///
        /// branchId có giá trị:
        /// chỉ lấy phiếu của chi nhánh đó.
        ///
        /// branchId null:
        /// lấy phiếu của toàn hệ thống.
        /// </summary>
        public async Task<
            List<InventoryImportTicketListDto>>
            GetInventoryImportTicketsAsync(
                int? branchId)
        {
            var query = _context
                .KhoImportTickets
                .AsNoTracking()
                .Include(ticket =>
                    ticket.Branch
                )
                .Include(ticket =>
                    ticket.Manager
                )
                .Include(ticket =>
                    ticket.Supplier
                )
                .Include(ticket =>
                    ticket.KhoImportDetails
                )
                .AsQueryable();

            if (
                branchId.HasValue &&
                branchId.Value > 0
            )
            {
                query = query.Where(ticket =>
                    ticket.BranchId ==
                    branchId.Value
                );
            }

            var tickets = await query
                .OrderByDescending(ticket =>
                    ticket.Id
                )
                .ToListAsync();

            return tickets
                .Select(ticket =>
                    new InventoryImportTicketListDto
                    {
                        Id = ticket.Id,

                        BranchId =
                            ticket.BranchId,

                        BranchName =
                            ticket.Branch?.Name ??
                            "Chưa rõ cơ sở",

                        ManagerName =
                            ticket.Manager?.FullName ??
                            "Chưa rõ người nhập",

                        SupplierName =
                            ticket.Supplier
                                ?.SupplierName ??
                            "Chưa rõ NCC",

                        InvoiceCode =
                            ticket.InvoiceCode,

                        InvoiceDate =
                            FormatDate(
                                ticket.InvoiceDate
                            ),

                        ImportDate =
                            FormatDateTime(
                                ticket.ImportDate
                            ),

                        TotalAmount =
                            ticket.TotalAmount,

                        TotalQuantity =
                            ticket.KhoImportDetails
                                .Sum(detail =>
                                    detail.Quantity
                                ),

                        ItemCount =
                            ticket.KhoImportDetails
                                .Count,

                        Note = ticket.Note
                    }
                )
                .ToList();
        }

        /// <summary>
        /// Lấy chi tiết một phiếu nhập kho.
        ///
        /// Khi branchId có giá trị, phiếu phải
        /// thuộc đúng chi nhánh đó.
        /// </summary>
        public async Task<
            InventoryImportTicketDetailDto?>
            GetInventoryImportTicketDetailAsync(
                int ticketId,
                int? branchId)
        {
            var query = _context
                .KhoImportTickets
                .AsNoTracking()
                .Include(ticket =>
                    ticket.Branch
                )
                .Include(ticket =>
                    ticket.Manager
                )
                .Include(ticket =>
                    ticket.Supplier
                )
                .Include(ticket =>
                    ticket.KhoImportDetails
                )
                    .ThenInclude(detail =>
                        detail.Product
                    )
                .AsQueryable();

            if (
                branchId.HasValue &&
                branchId.Value > 0
            )
            {
                query = query.Where(ticket =>
                    ticket.BranchId ==
                    branchId.Value
                );
            }

            var ticket = await query
                .FirstOrDefaultAsync(ticket =>
                    ticket.Id == ticketId
                );

            if (ticket == null)
            {
                return null;
            }

            return new InventoryImportTicketDetailDto
            {
                Id = ticket.Id,

                BranchId = ticket.BranchId,

                BranchName =
                    ticket.Branch?.Name ??
                    "Chưa rõ cơ sở",

                ManagerName =
                    ticket.Manager?.FullName ??
                    "Chưa rõ người nhập",

                SupplierName =
                    ticket.Supplier?.SupplierName ??
                    "Chưa rõ NCC",

                InvoiceCode =
                    ticket.InvoiceCode,

                InvoiceDate =
                    FormatDate(
                        ticket.InvoiceDate
                    ),

                ImportDate =
                    FormatDateTime(
                        ticket.ImportDate
                    ),

                TotalAmount =
                    ticket.TotalAmount,

                TotalQuantity =
                    ticket.KhoImportDetails
                        .Sum(detail =>
                            detail.Quantity
                        ),

                ItemCount =
                    ticket.KhoImportDetails.Count,

                Note = ticket.Note,

                Items = ticket.KhoImportDetails
                    .Select(detail =>
                        new InventoryImportTicketItemDto
                        {
                            ProductId =
                                detail.ProductId,

                            ProductCode =
                                detail.Product
                                    ?.ProductCode,

                            ProductName =
                                detail.Product
                                    ?.ProductName ??
                                "Chưa rõ sản phẩm",

                            Unit =
                                detail.UnitAtTime ??
                                detail.Product?.Unit,

                            Quantity =
                                detail.Quantity,

                            UnitPrice =
                                detail.UnitPrice ?? 0,

                            LineTotal =
                                detail.Quantity *
                                (detail.UnitPrice ?? 0)
                        }
                    )
                    .ToList()
            };
        }

        // =====================================================
        // LƯU DỮ LIỆU VÀ TRANSACTION
        // =====================================================

        /// <summary>
        /// Lưu toàn bộ thay đổi đang được
        /// theo dõi trong AppDbContext.
        /// </summary>
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Thực hiện một nhóm thao tác trong transaction.
        ///
        /// Thành công:
        /// commit toàn bộ thay đổi.
        ///
        /// Có lỗi:
        /// rollback toàn bộ thay đổi.
        /// </summary>
        public async Task<T>
            ExecuteInTransactionAsync<T>(
                Func<Task<T>> action)
        {
            await using var transaction =
                await _context.Database
                    .BeginTransactionAsync();

            try
            {
                var result = await action();

                await transaction.CommitAsync();

                return result;
            }
            catch
            {
                await transaction.RollbackAsync();

                throw;
            }
        }

        // =====================================================
        // HÀM ĐỊNH DẠNG
        // =====================================================

        /// <summary>
        /// Định dạng ngày giờ theo kiểu Việt Nam.
        /// </summary>
        private static string FormatDateTime(
            DateTime? value)
        {
            if (!value.HasValue)
            {
                return string.Empty;
            }

            return value.Value.ToString(
                "dd/MM/yyyy HH:mm"
            );
        }

        /// <summary>
        /// Định dạng ngày theo kiểu Việt Nam.
        /// </summary>
        private static string? FormatDate(
            DateOnly? value)
        {
            if (!value.HasValue)
            {
                return null;
            }

            return value.Value.ToString(
                "dd/MM/yyyy"
            );
        }
    }
}

using LuanVanTotNghiep.Models.Entities;
using LuanVanTotNghiep.DTOs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace LuanVanTotNghiep.Services
{
    public class KhoImportService
    {
        private readonly AppDbContext _context;

        public KhoImportService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> CreateImportTicketAsync(CreateImportTicketDto dto)
{
    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        // 1. Tạo Phiếu Nhập Kho tổng
        var ticket = new KhoImportTicket
        {
            ManagerId = dto.ManagerId,
            BranchId = dto.BranchId,
            SupplierId = dto.SupplierId,
            ImportDate = DateTime.Now
        };
        
        _context.KhoImportTickets.Add(ticket);
        await _context.SaveChangesAsync(); 

        // 2. Xử lý từng sản phẩm trong danh sách gửi lên
        foreach (var item in dto.Items)
        {
            int finalProductId = item.ProductId;

            // Kiểm tra: Nếu ProductId truyền lên bằng 0 hoặc không tồn tại trong DB, 
            // tiến hành xử lý tự động nhận diện / thêm mới theo Tên SP
            if (finalProductId <= 0)
            {
                var existingProduct = await _context.KhoProducts
                    .FirstOrDefaultAsync(p => p.ProductName.ToLower() == item.ProductName.ToLower());

                if (existingProduct != null)
                {
                    // Sản phẩm đã có sẵn -> Lấy ID cũ
                    finalProductId = existingProduct.Id;
                }
                else
                {
                    // Sản phẩm hoàn toàn mới -> Tự động thêm vào danh mục
                    var newProduct = new KhoProduct
                    {
                        ProductName = item.ProductName,
                        Unit = "Cái", // Đơn vị mặc định, Manager có thể sửa sau
                        SupplierId = dto.SupplierId
                    };
                    
                    _context.KhoProducts.Add(newProduct);
                    await _context.SaveChangesAsync(); // Lưu để DB tự động tăng và cấp `Id` mới
                    
                    finalProductId = newProduct.Id;
                }
            }

            // A. Lưu thông tin vào bảng chi tiết phiếu nhập
            var detail = new KhoImportDetail
            {
                ImportId = ticket.Id,
                ProductId = finalProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            };
            _context.KhoImportDetails.Add(detail);

            // B. Cập nhật tồn kho cho chi nhánh (Cộng dồn nếu đã có, tạo mới nếu chưa có)
            var inventory = await _context.KhoBranchInventories
                .FirstOrDefaultAsync(i => i.BranchId == dto.BranchId && i.ProductId == finalProductId);

            if (inventory != null)
            {
                inventory.Quantity += item.Quantity;
                _context.KhoBranchInventories.Update(inventory);
            }
            else
            {
                var newInventory = new KhoBranchInventory
                {
                    BranchId = dto.BranchId,
                    ProductId = finalProductId,
                    Quantity = item.Quantity
                };
                _context.KhoBranchInventories.Add(newInventory);
            }
        }

        // 3. Lưu toàn bộ thay đổi và Commit Transaction
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        
        return true;
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        throw new Exception("Lỗi hệ thống khi nhập kho: " + ex.Message);
    }
}
    }
}
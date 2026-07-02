using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.backend.Models.Entities;
using Microsoft.EntityFrameworkCore; // 👉 Bắt buộc phải có thư viện này để dùng ToListAsync() và Include()

namespace LuanVanTotNghiep.Repositories
{
    public class InventoryRepo
    {
        private readonly AppDbContext _context;

        public InventoryRepo(AppDbContext context)
        {
            _context = context;
        }

        // Lấy tồn kho toàn hệ thống (dành cho Admin) - ĐÃ KẾT NỐI DB
        public async Task<IEnumerable<InventoryDto>> GetAllInventoryAsync()
        {
            return await _context.KhoBranchInventories
                .Include(i => i.Branch)
                .Include(i => i.Product)
                .Select(i => new InventoryDto
                {
                    Id = i.Id, 
                    BranchName = i.Branch != null ? i.Branch.Name : "Chưa xác định", 
                    ProductName = i.Product != null ? i.Product.ProductName : "Chưa xác định",
                    Quantity = i.Quantity,
                    Unit = i.Product != null ? i.Product.Unit : "Cái"
                })
                .ToListAsync();
        }

        // Lấy tồn kho lọc theo cơ sở (dành cho Manager / Staff) - ĐÃ KẾT NỐI DB
        public async Task<IEnumerable<InventoryDto>> GetInventoryByBranchIdAsync(int branchId)
        {
            return await _context.KhoBranchInventories
                .Where(i => i.BranchId == branchId) // 👉 Lọc chặt chẽ theo ID cơ sở
                .Include(i => i.Branch)
                .Include(i => i.Product)
                .Select(i => new InventoryDto
                {
                    Id = i.Id, 
                    BranchName = i.Branch != null ? i.Branch.Name : "Chưa xác định", 
                    ProductName = i.Product != null ? i.Product.ProductName : "Chưa xác định",
                    Quantity = i.Quantity,
                    Unit = i.Product != null ? i.Product.Unit : "Cái"
                })
                .ToListAsync();
        }
    }
}
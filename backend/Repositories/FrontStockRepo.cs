using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Repositories
{
    public class FrontStockRepo
    {
        private readonly AppDbContext _context;

        public FrontStockRepo(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<InventoryDto>> GetAllFrontStockAsync()
        {
            return await _context.KhoBranchFrontStocks
                .AsNoTracking()
                .Include(i => i.Branch)
                .Include(i => i.Product)
                    .ThenInclude(p => p.Supplier)
                .OrderBy(i => i.Branch.Name)
                .ThenBy(i => i.Product.ProductName)
                .Select(i => new InventoryDto
                {
                    Id = i.Id,
                    BranchId = i.BranchId,
                    BranchName = i.Branch.Name,
                    ProductId = i.ProductId,
                    ProductCode = i.Product.ProductCode,
                    ProductName = i.Product.ProductName,
                    Unit = i.Product.Unit,
                    Quantity = i.Quantity,
                    SupplierName = i.Product.Supplier != null ? i.Product.Supplier.SupplierName : null
                })
                .ToListAsync();
        }

        public async Task<List<InventoryDto>> GetFrontStockByBranchIdAsync(int branchId)
        {
            return await _context.KhoBranchFrontStocks
                .AsNoTracking()
                .Include(i => i.Branch)
                .Include(i => i.Product)
                    .ThenInclude(p => p.Supplier)
                .Where(i => i.BranchId == branchId)
                .OrderBy(i => i.Product.ProductName)
                .Select(i => new InventoryDto
                {
                    Id = i.Id,
                    BranchId = i.BranchId,
                    BranchName = i.Branch.Name,
                    ProductId = i.ProductId,
                    ProductCode = i.Product.ProductCode,
                    ProductName = i.Product.ProductName,
                    Unit = i.Product.Unit,
                    Quantity = i.Quantity,
                    SupplierName = i.Product.Supplier != null ? i.Product.Supplier.SupplierName : null
                })
                .ToListAsync();
        }
    }
}
using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Repositories
{
    /// <summary>
    /// Thực hiện các truy vấn Database
    /// liên quan đến tồn quầy.
    /// </summary>
    public class FrontStockRepo
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// Nhận AppDbContext thông qua Dependency Injection.
        /// </summary>
        public FrontStockRepo(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy danh sách tồn quầy của toàn hệ thống.
        /// Hàm này được sử dụng khi Admin xem tất cả cơ sở.
        /// </summary>
        public async Task<List<InventoryDto>>
            GetAllFrontStockAsync()
        {
            return await _context
                .KhoBranchFrontStocks
                .AsNoTracking()
                .OrderBy(item =>
                    item.Branch.Name
                )
                .ThenBy(item =>
                    item.Product.ProductName
                )
                .Select(item => new InventoryDto
                {
                    Id = item.Id,

                    BranchId = item.BranchId,

                    BranchName =
                        item.Branch.Name,

                    ProductId = item.ProductId,

                    ProductCode =
                        item.Product.ProductCode,

                    ProductName =
                        item.Product.ProductName,

                    Unit = item.Product.Unit,

                    Quantity = item.Quantity,

                    SupplierName =
                        item.Product.Supplier != null
                            ? item.Product.Supplier
                                .SupplierName
                            : null
                })
                .ToListAsync();
        }

        /// <summary>
        /// Lấy danh sách tồn quầy
        /// của một chi nhánh cụ thể.
        ///
        /// Hàm này được sử dụng khi:
        /// - Manager xem cơ sở của mình.
        /// - Admin lọc theo một cơ sở.
        /// </summary>
        public async Task<List<InventoryDto>>
            GetFrontStockByBranchIdAsync(
                int branchId)
        {
            return await _context
                .KhoBranchFrontStocks
                .AsNoTracking()
                .Where(item =>
                    item.BranchId == branchId
                )
                .OrderBy(item =>
                    item.Product.ProductName
                )
                .Select(item => new InventoryDto
                {
                    Id = item.Id,

                    BranchId = item.BranchId,

                    BranchName =
                        item.Branch.Name,

                    ProductId = item.ProductId,

                    ProductCode =
                        item.Product.ProductCode,

                    ProductName =
                        item.Product.ProductName,

                    Unit = item.Product.Unit,

                    Quantity = item.Quantity,

                    SupplierName =
                        item.Product.Supplier != null
                            ? item.Product.Supplier
                                .SupplierName
                            : null
                })
                .ToListAsync();
        }
    }
}
using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Repositories;

namespace LuanVanTotNghiep.Services
{
    /// <summary>
    /// Xử lý nghiệp vụ liên quan đến tồn quầy.
    ///
    /// Service nằm giữa Controller và Repository:
    /// Controller -> Service -> Repository -> Database.
    /// </summary>
    public class FrontStockService
    {
        private readonly FrontStockRepo _frontStockRepo;

        /// <summary>
        /// Nhận FrontStockRepo thông qua Dependency Injection.
        /// </summary>
        public FrontStockService(FrontStockRepo frontStockRepo)
        {
            _frontStockRepo = frontStockRepo;
        }

        /// <summary>
        /// Lấy danh sách tồn quầy dựa theo quyền người dùng.
        ///
        /// Admin:
        /// - Có branchId: xem một chi nhánh.
        /// - Không có branchId: xem toàn bộ hệ thống.
        ///
        /// Manager:
        /// - Chỉ được xem chi nhánh có trong token.
        /// - Không sử dụng branchId do Frontend truyền lên.
        /// </summary>
        public async Task<List<InventoryDto>> GetFrontStockAsync(
            bool isAdmin,
            int? tokenBranchId,
            int? requestedBranchId)
        {
            // Người dùng không phải Admin chỉ được xem
            // dữ liệu thuộc chi nhánh trong token.
            if (!isAdmin)
            {
                if (!tokenBranchId.HasValue ||
                    tokenBranchId.Value <= 0)
                {
                    throw new UnauthorizedAccessException(
                        "Không tìm thấy thông tin chi nhánh trong token."
                    );
                }

                return await _frontStockRepo
                    .GetFrontStockByBranchIdAsync(
                        tokenBranchId.Value
                    );
            }

            // Admin có truyền branchId hợp lệ
            // thì chỉ lấy tồn quầy của chi nhánh đó.
            if (requestedBranchId.HasValue &&
                requestedBranchId.Value > 0)
            {
                return await _frontStockRepo
                    .GetFrontStockByBranchIdAsync(
                        requestedBranchId.Value
                    );
            }

            // Admin không truyền branchId
            // thì lấy tồn quầy toàn hệ thống.
            return await _frontStockRepo
                .GetAllFrontStockAsync();
        }
    }
}
using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Repositories;

namespace LuanVanTotNghiep.Services
{
    /// <summary>
    /// Xử lý nghiệp vụ liên quan đến tồn kho chi nhánh.
    ///
    /// Luồng xử lý:
    /// Controller -> Service -> Repository -> Database.
    /// </summary>
    public class InventoryService
    {
        private readonly InventoryRepo _inventoryRepo;

        /// <summary>
        /// Nhận InventoryRepo thông qua Dependency Injection.
        /// </summary>
        public InventoryService(
            InventoryRepo inventoryRepo)
        {
            _inventoryRepo = inventoryRepo;
        }

        /// <summary>
        /// Lấy danh sách tồn kho theo quyền người dùng.
        ///
        /// Admin:
        /// - Có requestedBranchId: xem một chi nhánh.
        /// - Không có requestedBranchId: xem toàn hệ thống.
        ///
        /// Manager hoặc Staff:
        /// - Chỉ xem chi nhánh có trong token.
        /// - Không sử dụng branchId do Frontend truyền lên.
        /// </summary>
        public async Task<List<InventoryDto>>
            GetInventoryAsync(
                bool isAdmin,
                int? tokenBranchId,
                int? requestedBranchId)
        {
            // Người dùng không phải Admin chỉ được
            // xem tồn kho thuộc chi nhánh trong token.
            if (!isAdmin)
            {
                if (!tokenBranchId.HasValue ||
                    tokenBranchId.Value <= 0)
                {
                    throw new UnauthorizedAccessException(
                        "Không tìm thấy thông tin chi nhánh trong token."
                    );
                }

                return await _inventoryRepo
                    .GetInventoryByBranchIdAsync(
                        tokenBranchId.Value
                    );
            }

            // Admin có chọn một chi nhánh cụ thể.
            if (requestedBranchId.HasValue &&
                requestedBranchId.Value > 0)
            {
                return await _inventoryRepo
                    .GetInventoryByBranchIdAsync(
                        requestedBranchId.Value
                    );
            }

            // Admin không chọn chi nhánh
            // thì lấy tồn kho toàn hệ thống.
            return await _inventoryRepo
                .GetAllInventoryAsync();
        }
    }
}
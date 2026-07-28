using LuanVanTotNghiep.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LuanVanTotNghiep.Controllers
{
    /// <summary>
    /// Cung cấp API xem tồn kho chi nhánh.
    ///
    /// Controller chịu trách nhiệm:
    /// - Nhận tham số từ request.
    /// - Đọc thông tin từ JWT.
    /// - Gọi InventoryService.
    /// - Trả kết quả HTTP.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class InventoryController : ControllerBase
    {
        private readonly InventoryService
            _inventoryService;

        /// <summary>
        /// Nhận InventoryService thông qua
        /// Dependency Injection.
        /// </summary>
        public InventoryController(
            InventoryService inventoryService)
        {
            _inventoryService =
                inventoryService;
        }

        /// <summary>
        /// Lấy danh sách tồn kho.
        ///
        /// Admin:
        /// GET /api/Inventory
        /// -> Xem tồn kho toàn hệ thống.
        ///
        /// Admin:
        /// GET /api/Inventory?branchId=1
        /// -> Xem tồn kho của chi nhánh 1.
        ///
        /// Manager hoặc Staff:
        /// GET /api/Inventory
        /// -> Backend tự lấy chi nhánh từ token.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Get(
            [FromQuery] int? branchId)
        {
            try
            {
                // Đọc vai trò người dùng từ JWT.
                var role = GetClaimValue(
                    ClaimTypes.Role,
                    "role",
                    "Role"
                )?.Trim().ToUpperInvariant();

                // Kiểm tra người dùng có phải
                // Admin hay không.
                var isAdmin =
                    role == "ADMIN" ||
                    role == "QUẢN TRỊ" ||
                    role == "QUAN TRI";

                int? tokenBranchId = null;

                // Người dùng không phải Admin phải có
                // thông tin chi nhánh trong token.
                if (!isAdmin)
                {
                    var tokenBranchIdString =
                        GetClaimValue(
                            "BranchId",
                            "branchId",
                            "branch_id"
                        );

                    if (int.TryParse(
                            tokenBranchIdString,
                            out var parsedBranchId) &&
                        parsedBranchId > 0)
                    {
                        tokenBranchId =
                            parsedBranchId;
                    }
                }

                // Gọi Service xử lý quyền
                // và lấy dữ liệu tồn kho.
                var data =
                    await _inventoryService
                        .GetInventoryAsync(
                            isAdmin,
                            tokenBranchId,
                            branchId
                        );

                return Ok(data);
            }
            catch (UnauthorizedAccessException ex)
            {
                // Token không chứa chi nhánh hợp lệ.
                return Unauthorized(new
                {
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                // Ghi lỗi ra cửa sổ Backend
                // để hỗ trợ quá trình phát triển.
                Console.WriteLine(
                    $"Lỗi tải tồn kho: {ex}"
                );

                return StatusCode(500, new
                {
                    message =
                        "Đã xảy ra lỗi khi tải dữ liệu tồn kho."
                });
            }
        }

        /// <summary>
        /// Tìm giá trị claim theo nhiều tên khác nhau.
        ///
        /// Ví dụ:
        /// BranchId, branchId hoặc branch_id.
        /// </summary>
        private string? GetClaimValue(
            params string[] claimTypes)
        {
            foreach (var claimType in claimTypes)
            {
                var value = User.Claims
                    .FirstOrDefault(
                        claim =>
                            claim.Type == claimType
                    )
                    ?.Value;

                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return null;
        }
    }
}
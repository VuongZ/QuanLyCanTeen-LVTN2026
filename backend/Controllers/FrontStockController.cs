using LuanVanTotNghiep.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LuanVanTotNghiep.Controllers
{
    /// <summary>
    /// Cung cấp API xem tồn quầy.
    ///
    /// Controller chỉ thực hiện:
    /// - Đọc thông tin từ token.
    /// - Nhận tham số từ request.
    /// - Gọi FrontStockService.
    /// - Trả kết quả HTTP.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FrontStockController : ControllerBase
    {
        private readonly FrontStockService _frontStockService;

        /// <summary>
        /// Nhận FrontStockService thông qua Dependency Injection.
        /// </summary>
        public FrontStockController(
            FrontStockService frontStockService)
        {
            _frontStockService = frontStockService;
        }

        /// <summary>
        /// Lấy danh sách tồn quầy.
        ///
        /// Admin:
        /// GET /api/FrontStock
        /// -> Lấy tồn quầy toàn hệ thống.
        ///
        /// Admin:
        /// GET /api/FrontStock?branchId=1
        /// -> Lấy tồn quầy của chi nhánh 1.
        ///
        /// Manager:
        /// GET /api/FrontStock
        /// -> Backend tự lấy chi nhánh từ token.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Get(
            [FromQuery] int? branchId)
        {
            try
            {
                // Lấy vai trò của người dùng từ token.
                var role = GetClaimValue(
                    ClaimTypes.Role,
                    "role",
                    "Role"
                )?.Trim().ToUpperInvariant();

                // Kiểm tra người dùng có phải Admin hay không.
                var isAdmin =
                    role == "ADMIN" ||
                    role == "QUẢN TRỊ" ||
                    role == "QUAN TRI";

                int? tokenBranchId = null;

                // Người dùng không phải Admin cần có
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
                        tokenBranchId = parsedBranchId;
                    }
                }

                // Gọi Service để xử lý quyền
                // và lấy dữ liệu tồn quầy.
                var data =
                    await _frontStockService
                        .GetFrontStockAsync(
                            isAdmin,
                            tokenBranchId,
                            branchId
                        );

                return Ok(data);
            }
            catch (UnauthorizedAccessException ex)
            {
                // Trả về 401 khi token không có
                // thông tin chi nhánh hợp lệ.
                return Unauthorized(new
                {
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Lỗi tải tồn quầy: {ex}"
                );

                return StatusCode(500, new
                {
                    message =
                        "Đã xảy ra lỗi khi tải dữ liệu tồn quầy."
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
using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LuanVanTotNghiep.Controllers
{
    /// <summary>
    /// Cung cấp các API liên quan đến
    /// xuất hàng từ kho chi nhánh ra quầy.
    ///
    /// Controller chịu trách nhiệm:
    /// - Xác thực và phân quyền.
    /// - Đọc thông tin người dùng từ JWT.
    /// - Nhận dữ liệu từ request.
    /// - Gọi KhoExportService.
    /// - Trả kết quả HTTP cho Frontend.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class KhoExportController : ControllerBase
    {
        private readonly KhoExportService _exportService;

        /// <summary>
        /// Nhận KhoExportService thông qua
        /// Dependency Injection.
        /// </summary>
        public KhoExportController(
            KhoExportService exportService)
        {
            _exportService = exportService;
        }

        /// <summary>
        /// Lấy danh sách ca làm trong ngày
        /// mà Manager có thể xuất hàng ra quầy.
        ///
        /// ManagerId luôn được lấy từ JWT,
        /// không sử dụng ID do Frontend truyền lên.
        /// </summary>
        [HttpGet("available-schedules")]
        [Authorize(Roles = "MANAGER")]
        public async Task<IActionResult>
            GetAvailableSchedules()
        {
            // Lấy ID người dùng từ JWT.
            var tokenUserId = GetIntClaim(
                ClaimTypes.NameIdentifier,
                "UserId",
                "userId",
                "id",
                "Id"
            );

            if (
                !tokenUserId.HasValue ||
                tokenUserId.Value <= 0
            )
            {
                return Unauthorized(new
                {
                    message =
                        "Không tìm thấy thông tin người dùng trong token."
                });
            }

            try
            {
                var data =
                    await _exportService
                        .GetTodayExportSchedulesAsync(
                            tokenUserId.Value
                        );

                return Ok(data);
            }
            catch (InvalidOperationException ex)
            {
                // Lỗi nghiệp vụ như tài khoản
                // không phải Manager hoặc chưa có chi nhánh.
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Lỗi lấy ca xuất hàng: {ex}"
                );

                return StatusCode(500, new
                {
                    message =
                        "Đã xảy ra lỗi khi lấy danh sách ca xuất hàng."
                });
            }
        }

        /// <summary>
        /// Tạo phiếu xuất hàng từ kho
        /// chi nhánh ra quầy.
        ///
        /// Chỉ Manager được thực hiện.
        ///
        /// ManagerId và BranchId được lấy từ JWT,
        /// không sử dụng dữ liệu do Frontend gửi lên.
        /// </summary>
        [HttpPost("submit-export")]
        [Authorize(Roles = "MANAGER")]
        public async Task<IActionResult>
            SubmitExportTicket(
                [FromBody] CreateExportTicketDto dto)
        {
            // Kiểm tra danh sách sản phẩm.
            if (
                dto.Items == null ||
                dto.Items.Count == 0
            )
            {
                return BadRequest(new
                {
                    message =
                        "Phiếu xuất không có sản phẩm nào."
                });
            }

            // Lấy ID Manager từ JWT.
            var tokenUserId = GetIntClaim(
                ClaimTypes.NameIdentifier,
                "UserId",
                "userId",
                "id",
                "Id"
            );

            if (
                !tokenUserId.HasValue ||
                tokenUserId.Value <= 0
            )
            {
                return Unauthorized(new
                {
                    message =
                        "Không xác định được người dùng từ token."
                });
            }

            // Lấy chi nhánh của Manager từ JWT.
            var tokenBranchId = GetIntClaim(
                "BranchId",
                "branchId",
                "branch_id"
            );

            if (
                !tokenBranchId.HasValue ||
                tokenBranchId.Value <= 0
            )
            {
                return Unauthorized(new
                {
                    message =
                        "Không tìm thấy thông tin chi nhánh trong token."
                });
            }

            // Ghi đè dữ liệu Frontend bằng
            // thông tin đáng tin cậy trong JWT.
            dto.ManagerId = tokenUserId.Value;
            dto.BranchId = tokenBranchId.Value;

            try
            {
                var ticketId =
                    await _exportService
                        .CreateExportTicketAsync(dto);

                return Ok(new
                {
                    message =
                        "Xuất hàng ra quầy thành công!",

                    exportTicketId =
                        ticketId
                });
            }
            catch (InvalidOperationException ex)
            {
                // Các lỗi nghiệp vụ:
                // - Ca làm không hợp lệ.
                // - Ngoài thời gian xuất hàng.
                // - Sản phẩm không đủ tồn kho.
                // - Manager không thuộc chi nhánh.
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Lỗi tạo phiếu xuất kho: {ex}"
                );

                return StatusCode(500, new
                {
                    message =
                        "Đã xảy ra lỗi khi xuất hàng ra quầy."
                });
            }
        }

        /// <summary>
        /// Lấy danh sách lịch sử phiếu xuất ra quầy.
        ///
        /// Admin:
        /// - Không truyền branchId: xem toàn hệ thống.
        /// - Có branchId: xem một chi nhánh.
        ///
        /// Manager:
        /// - Chỉ xem phiếu thuộc chi nhánh trong JWT.
        /// </summary>
        [HttpGet("front-stock-tickets")]
        [Authorize(Roles = "ADMIN,MANAGER")]
        public async Task<IActionResult>
            GetFrontStockExportTickets(
                [FromQuery] int? branchId)
        {
            try
            {
                var finalBranchId =
                    ResolveBranchIdForQuery(
                        branchId
                    );

                // -1 nghĩa là tài khoản không có
                // BranchId hợp lệ trong JWT.
                if (finalBranchId == -1)
                {
                    return Unauthorized(new
                    {
                        message =
                            "Không tìm thấy thông tin chi nhánh trong token."
                    });
                }

                // 0 nghĩa là Admin
                // đang xem toàn hệ thống.
                int? filterBranchId =
                    finalBranchId == 0
                        ? null
                        : finalBranchId;

                var data =
                    await _exportService
                        .GetFrontStockExportTicketsAsync(
                            filterBranchId
                        );

                return Ok(data);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Lỗi lấy lịch sử phiếu xuất: {ex}"
                );

                return StatusCode(500, new
                {
                    message =
                        "Đã xảy ra lỗi khi lấy danh sách phiếu xuất ra quầy."
                });
            }
        }

        /// <summary>
        /// Lấy chi tiết một phiếu xuất ra quầy.
        ///
        /// Manager chỉ được xem phiếu
        /// thuộc chi nhánh trong JWT.
        /// </summary>
        [HttpGet("front-stock-tickets/{id:int}")]
        [Authorize(Roles = "ADMIN,MANAGER")]
        public async Task<IActionResult>
            GetFrontStockExportTicketDetail(
                int id,
                [FromQuery] int? branchId)
        {
            // Kiểm tra ID phiếu.
            if (id <= 0)
            {
                return BadRequest(new
                {
                    message =
                        "Mã phiếu xuất không hợp lệ."
                });
            }

            try
            {
                var finalBranchId =
                    ResolveBranchIdForQuery(
                        branchId
                    );

                if (finalBranchId == -1)
                {
                    return Unauthorized(new
                    {
                        message =
                            "Không tìm thấy thông tin chi nhánh trong token."
                    });
                }

                int? filterBranchId =
                    finalBranchId == 0
                        ? null
                        : finalBranchId;

                var data =
                    await _exportService
                        .GetFrontStockExportTicketDetailAsync(
                            id,
                            filterBranchId
                        );

                if (data == null)
                {
                    return NotFound(new
                    {
                        message =
                            "Không tìm thấy phiếu xuất ra quầy."
                    });
                }

                return Ok(data);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Lỗi lấy chi tiết phiếu xuất: {ex}"
                );

                return StatusCode(500, new
                {
                    message =
                        "Đã xảy ra lỗi khi lấy chi tiết phiếu xuất ra quầy."
                });
            }
        }

        /// <summary>
        /// Xác định chi nhánh người dùng
        /// được phép truy vấn.
        ///
        /// Kết quả:
        /// - 0: Admin xem toàn hệ thống.
        /// - Số dương: lọc theo chi nhánh.
        /// - -1: không có BranchId hợp lệ.
        /// </summary>
        private int ResolveBranchIdForQuery(
            int? requestedBranchId)
        {
            var role = GetClaimValue(
                ClaimTypes.Role,
                "role",
                "Role"
            )?.Trim().ToUpperInvariant();

            var isAdmin =
                role == "ADMIN" ||
                role == "QUẢN TRỊ" ||
                role == "QUAN TRI";

            // Admin được chọn chi nhánh
            // hoặc xem toàn hệ thống.
            if (isAdmin)
            {
                return
                    requestedBranchId.HasValue &&
                    requestedBranchId.Value > 0
                        ? requestedBranchId.Value
                        : 0;
            }

            // Manager luôn lấy BranchId từ JWT,
            // bỏ qua branchId do Frontend truyền lên.
            var tokenBranchId =
                GetIntClaim(
                    "BranchId",
                    "branchId",
                    "branch_id"
                );

            if (
                !tokenBranchId.HasValue ||
                tokenBranchId.Value <= 0
            )
            {
                return -1;
            }

            return tokenBranchId.Value;
        }

        /// <summary>
        /// Tìm claim có giá trị số nguyên
        /// theo nhiều tên khác nhau.
        /// </summary>
        private int? GetIntClaim(
            params string[] claimTypes)
        {
            foreach (var claimType in claimTypes)
            {
                var value = User.Claims
                    .FirstOrDefault(claim =>
                        claim.Type == claimType
                    )
                    ?.Value;

                if (
                    int.TryParse(
                        value,
                        out var result
                    )
                )
                {
                    return result;
                }
            }

            return null;
        }

        /// <summary>
        /// Tìm giá trị claim theo nhiều
        /// tên thuộc tính khác nhau.
        /// </summary>
        private string? GetClaimValue(
            params string[] claimTypes)
        {
            foreach (var claimType in claimTypes)
            {
                var value = User.Claims
                    .FirstOrDefault(claim =>
                        claim.Type == claimType
                    )
                    ?.Value;

                if (
                    !string.IsNullOrWhiteSpace(
                        value
                    )
                )
                {
                    return value;
                }
            }

            return null;
        }
    }
}
using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LuanVanTotNghiep.Controllers
{
    /// <summary>
    /// Cung cấp các API liên quan đến
    /// báo cáo kiểm kê và kết ca.
    ///
    /// Controller chịu trách nhiệm:
    /// - Xác thực và phân quyền.
    /// - Lấy người dùng và chi nhánh từ JWT.
    /// - Nhận dữ liệu từ request.
    /// - Gọi ShiftClosingService.
    /// - Trả kết quả HTTP cho Frontend.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ShiftClosingController : ControllerBase
    {
        private readonly ShiftClosingService
            _shiftClosingService;

        private readonly ILogger<ShiftClosingController>
            _logger;

        /// <summary>
        /// Nhận ShiftClosingService và ILogger
        /// thông qua Dependency Injection.
        /// </summary>
        public ShiftClosingController(
            ShiftClosingService shiftClosingService,
            ILogger<ShiftClosingController> logger)
        {
            _shiftClosingService =
                shiftClosingService;

            _logger =
                logger;
        }

        // =====================================================
        // STAFF
        // =====================================================

        /// <summary>
        /// Lấy ca làm trong ngày mà Staff
        /// cần thực hiện báo cáo kết ca.
        /// </summary>
        [HttpGet("today-shift")]
        [Authorize(Roles = "STAFF")]
        public async Task<IActionResult>
            GetTodayShift()
        {
            var staffId =
                GetCurrentUserId();

            if (staffId <= 0)
            {
                return Unauthorized(new
                {
                    message =
                        "Không tìm thấy thông tin nhân viên trong token."
                });
            }

            try
            {
                var data =
                    await _shiftClosingService
                        .GetTodayClosingShiftAsync(
                            staffId
                        );

                if (data == null)
                {
                    return NotFound(new
                    {
                        message =
                            "Hôm nay bạn chưa có ca làm cần báo cáo kết ca."
                    });
                }

                return Ok(data);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Lỗi khi lấy ca kết ca của Staff {StaffId}.",
                    staffId
                );

                return StatusCode(500, new
                {
                    message =
                        "Đã xảy ra lỗi khi lấy thông tin ca kết ca."
                });
            }
        }

        /// <summary>
        /// Lấy tồn quầy của chi nhánh
        /// để Staff thực hiện kiểm kê.
        /// </summary>
        [HttpGet("front-stock")]
        [Authorize(Roles = "STAFF")]
        public async Task<IActionResult>
            GetFrontStockForClosing()
        {
            var staffId =
                GetCurrentUserId();

            if (staffId <= 0)
            {
                return Unauthorized(new
                {
                    message =
                        "Không tìm thấy thông tin nhân viên trong token."
                });
            }

            try
            {
                var data =
                    await _shiftClosingService
                        .GetFrontStockForClosingAsync(
                            staffId
                        );

                return Ok(data);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Lỗi khi lấy tồn quầy kết ca của Staff {StaffId}.",
                    staffId
                );

                return StatusCode(500, new
                {
                    message =
                        "Đã xảy ra lỗi khi lấy tồn quầy kết ca."
                });
            }
        }

        /// <summary>
        /// Staff gửi báo cáo kiểm kê kết ca.
        ///
        /// StaffId luôn được lấy từ JWT,
        /// không nhận từ Frontend.
        /// </summary>
        [HttpPost("submit")]
        [Authorize(Roles = "STAFF")]
        public async Task<IActionResult>
            SubmitClosingReport(
                [FromBody] SubmitShiftClosingDto dto)
        {
            if (dto == null)
            {
                return BadRequest(new
                {
                    message =
                        "Dữ liệu báo cáo kết ca không hợp lệ."
                });
            }

            var staffId =
                GetCurrentUserId();

            if (staffId <= 0)
            {
                return Unauthorized(new
                {
                    message =
                        "Không tìm thấy thông tin nhân viên trong token."
                });
            }

            try
            {
                var reportId =
                    await _shiftClosingService
                        .SubmitShiftClosingReportAsync(
                            staffId,
                            dto
                        );

                return Ok(new
                {
                    message =
                        "Đã gửi báo cáo kết ca và đang chờ Quản lý duyệt. " +
                        "Các nhân viên khác trong cùng ca tạm thời không thể gửi thêm báo cáo.",

                    reportId
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Lỗi khi Staff {StaffId} gửi báo cáo kết ca.",
                    staffId
                );

                return StatusCode(500, new
                {
                    message =
                        "Đã xảy ra lỗi khi gửi báo cáo kết ca."
                });
            }
        }

        /// <summary>
        /// Lấy lịch sử báo cáo kết ca
        /// của Staff đang đăng nhập.
        /// </summary>
        [HttpGet("my-reports")]
        [Authorize(Roles = "STAFF")]
        public async Task<IActionResult>
            GetMyReports()
        {
            var staffId =
                GetCurrentUserId();

            if (staffId <= 0)
            {
                return Unauthorized(new
                {
                    message =
                        "Không tìm thấy thông tin nhân viên trong token."
                });
            }

            try
            {
                var data =
                    await _shiftClosingService
                        .GetMyReportsAsync(
                            staffId
                        );

                return Ok(data);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Lỗi khi lấy lịch sử kết ca của Staff {StaffId}.",
                    staffId
                );

                return StatusCode(500, new
                {
                    message =
                        "Đã xảy ra lỗi khi lấy lịch sử kết ca."
                });
            }
        }

        /// <summary>
        /// Lấy chi tiết một báo cáo kết ca
        /// thuộc về Staff đang đăng nhập.
        /// </summary>
        [HttpGet("my-reports/{id:int}")]
        [Authorize(Roles = "STAFF")]
        public async Task<IActionResult>
            GetMyReportDetail(
                int id)
        {
            if (id <= 0)
            {
                return BadRequest(new
                {
                    message =
                        "Mã báo cáo kết ca không hợp lệ."
                });
            }

            var staffId =
                GetCurrentUserId();

            if (staffId <= 0)
            {
                return Unauthorized(new
                {
                    message =
                        "Không tìm thấy thông tin nhân viên trong token."
                });
            }

            try
            {
                var data =
                    await _shiftClosingService
                        .GetMyReportDetailAsync(
                            staffId,
                            id
                        );

                if (data == null)
                {
                    return NotFound(new
                    {
                        message =
                            "Không tìm thấy báo cáo kết ca."
                    });
                }

                return Ok(data);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Lỗi khi Staff {StaffId} lấy báo cáo kết ca {ReportId}.",
                    staffId,
                    id
                );

                return StatusCode(500, new
                {
                    message =
                        "Đã xảy ra lỗi khi lấy chi tiết báo cáo kết ca."
                });
            }
        }

        // =====================================================
        // MANAGER VÀ ADMIN
        // =====================================================

        /// <summary>
        /// Lấy danh sách báo cáo kết ca
        /// dành cho Manager hoặc Admin.
        ///
        /// Admin:
        /// - Không truyền branchId: xem toàn hệ thống.
        /// - Có branchId: xem một chi nhánh.
        ///
        /// Manager:
        /// - Chỉ xem chi nhánh trong JWT.
        /// </summary>
        [HttpGet("reports")]
        [Authorize(Roles = "ADMIN,MANAGER")]
        public async Task<IActionResult>
            GetReportsForManagement(
                [FromQuery] int? branchId)
        {
            if (
                !TryResolveManagementBranchId(
                    branchId,
                    out var finalBranchId
                )
            )
            {
                return Unauthorized(new
                {
                    message =
                        "Không tìm thấy thông tin cơ sở trong token."
                });
            }

            try
            {
                var data =
                    await _shiftClosingService
                        .GetReportsForManagementAsync(
                            finalBranchId
                        );

                return Ok(data);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Lỗi khi lấy danh sách báo cáo kết ca."
                );

                return StatusCode(500, new
                {
                    message =
                        "Đã xảy ra lỗi khi lấy danh sách báo cáo kết ca."
                });
            }
        }

        /// <summary>
        /// Lấy chi tiết báo cáo kết ca
        /// dành cho Manager hoặc Admin.
        /// </summary>
        [HttpGet("reports/{id:int}")]
        [Authorize(Roles = "ADMIN,MANAGER")]
        public async Task<IActionResult>
            GetReportDetailForManagement(
                int id,
                [FromQuery] int? branchId)
        {
            if (id <= 0)
            {
                return BadRequest(new
                {
                    message =
                        "Mã báo cáo kết ca không hợp lệ."
                });
            }

            if (
                !TryResolveManagementBranchId(
                    branchId,
                    out var finalBranchId
                )
            )
            {
                return Unauthorized(new
                {
                    message =
                        "Không tìm thấy thông tin cơ sở trong token."
                });
            }

            try
            {
                var data =
                    await _shiftClosingService
                        .GetReportDetailForManagementAsync(
                            id,
                            finalBranchId
                        );

                if (data == null)
                {
                    return NotFound(new
                    {
                        message =
                            "Không tìm thấy báo cáo kết ca."
                    });
                }

                return Ok(data);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Lỗi khi lấy chi tiết báo cáo kết ca {ReportId}.",
                    id
                );

                return StatusCode(500, new
                {
                    message =
                        "Đã xảy ra lỗi khi lấy chi tiết báo cáo kết ca."
                });
            }
        }

        // =====================================================
        // MANAGER DUYỆT VÀ TỪ CHỐI
        // =====================================================

        /// <summary>
        /// Manager duyệt báo cáo kết ca.
        ///
        /// ManagerId được lấy từ JWT.
        /// </summary>
        [HttpPut("reports/{id:int}/approve")]
        [Authorize(Roles = "MANAGER")]
        public async Task<IActionResult>
            ApproveReport(
                int id)
        {
            if (id <= 0)
            {
                return BadRequest(new
                {
                    message =
                        "Mã báo cáo kết ca không hợp lệ."
                });
            }

            var managerId =
                GetCurrentUserId();

            if (managerId <= 0)
            {
                return Unauthorized(new
                {
                    message =
                        "Không tìm thấy thông tin Quản lý trong token."
                });
            }

            try
            {
                await _shiftClosingService
                    .ApproveReportAsync(
                        managerId,
                        id
                    );

                return Ok(new
                {
                    message =
                        "Duyệt báo cáo thành công. " +
                        "Tồn quầy đã được cập nhật và các nhân viên trong ca có thể checkout."
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Lỗi khi Manager {ManagerId} duyệt báo cáo {ReportId}.",
                    managerId,
                    id
                );

                return StatusCode(500, new
                {
                    message =
                        "Đã xảy ra lỗi khi duyệt báo cáo kết ca."
                });
            }
        }

        /// <summary>
        /// Manager từ chối báo cáo kết ca.
        ///
        /// Việc từ chối không làm thay đổi tồn quầy.
        /// </summary>
        [HttpPut("reports/{id:int}/reject")]
        [Authorize(Roles = "MANAGER")]
        public async Task<IActionResult>
            RejectReport(
                int id,
                [FromBody] RejectShiftClosingDto dto)
        {
            if (id <= 0)
            {
                return BadRequest(new
                {
                    message =
                        "Mã báo cáo kết ca không hợp lệ."
                });
            }

            if (dto == null)
            {
                return BadRequest(new
                {
                    message =
                        "Dữ liệu từ chối báo cáo không hợp lệ."
                });
            }

            var managerId =
                GetCurrentUserId();

            if (managerId <= 0)
            {
                return Unauthorized(new
                {
                    message =
                        "Không tìm thấy thông tin Quản lý trong token."
                });
            }

            try
            {
                await _shiftClosingService
                    .RejectReportAsync(
                        managerId,
                        id,
                        dto.Reason
                    );

                return Ok(new
                {
                    message =
                        "Đã từ chối báo cáo. " +
                        "Tồn quầy được giữ nguyên và tất cả nhân viên trong ca có thể gửi lại báo cáo."
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Lỗi khi Manager {ManagerId} từ chối báo cáo {ReportId}.",
                    managerId,
                    id
                );

                return StatusCode(500, new
                {
                    message =
                        "Đã xảy ra lỗi khi từ chối báo cáo kết ca."
                });
            }
        }

        // =====================================================
        // JWT HELPERS
        // =====================================================

        /// <summary>
        /// Xác định chi nhánh mà Manager hoặc Admin
        /// được phép dùng để truy vấn báo cáo.
        ///
        /// Admin:
        /// - branchId null: toàn hệ thống.
        /// - branchId có giá trị: một chi nhánh.
        ///
        /// Manager:
        /// - Luôn sử dụng BranchId trong JWT.
        /// </summary>
        private bool TryResolveManagementBranchId(
            int? requestedBranchId,
            out int? resolvedBranchId)
        {
            if (User.IsInRole("ADMIN"))
            {
                resolvedBranchId =
                    requestedBranchId.HasValue &&
                    requestedBranchId.Value > 0
                        ? requestedBranchId.Value
                        : null;

                return true;
            }

            if (!User.IsInRole("MANAGER"))
            {
                resolvedBranchId = null;

                return false;
            }

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
                resolvedBranchId = null;

                return false;
            }

            resolvedBranchId =
                tokenBranchId.Value;

            return true;
        }

        /// <summary>
        /// Lấy ID người dùng đăng nhập
        /// từ JWT.
        /// </summary>
        private int GetCurrentUserId()
        {
            var userId =
                GetIntClaim(
                    ClaimTypes.NameIdentifier,
                    "UserId",
                    "userId",
                    "id",
                    "Id"
                );

            return userId.HasValue &&
                   userId.Value > 0
                ? userId.Value
                : 0;
        }

        /// <summary>
        /// Tìm claim số nguyên theo
        /// nhiều tên claim khác nhau.
        /// </summary>
        private int? GetIntClaim(
            params string[] claimTypes)
        {
            foreach (var claimType in claimTypes)
            {
                var value =
                    User.FindFirst(
                        claimType
                    )?.Value;

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
    }
}
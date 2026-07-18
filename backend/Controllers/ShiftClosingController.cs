using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LuanVanTotNghiep.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ShiftClosingController : ControllerBase
    {
        private readonly ShiftClosingService _shiftClosingService;

        public ShiftClosingController(ShiftClosingService shiftClosingService)
        {
            _shiftClosingService = shiftClosingService;
        }

        [HttpGet("today-shift")]
        public async Task<IActionResult> GetTodayShift()
        {
            try
            {
                var staffId = GetCurrentUserId();

                if (staffId <= 0)
                    return Unauthorized(new { message = "Không tìm thấy thông tin nhân viên trong token." });

                var data = await _shiftClosingService.GetTodayClosingShiftAsync(staffId);

                if (data == null)
                    return NotFound(new { message = "Hôm nay bạn chưa có ca làm cần báo cáo kết ca." });

                return Ok(data);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi lấy ca kết ca: " + ex.Message });
            }
        }

        [HttpGet("front-stock")]
        public async Task<IActionResult> GetFrontStockForClosing()
        {
            try
            {
                var staffId = GetCurrentUserId();

                if (staffId <= 0)
                    return Unauthorized(new { message = "Không tìm thấy thông tin nhân viên trong token." });

                var data = await _shiftClosingService.GetFrontStockForClosingAsync(staffId);
                return Ok(data);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi lấy tồn quầy kết ca: " + ex.Message });
            }
        }

        [HttpPost("submit")]
        public async Task<IActionResult> SubmitClosingReport([FromBody] SubmitShiftClosingDto dto)
        {
            try
            {
                var staffId = GetCurrentUserId();

                if (staffId <= 0)
                    return Unauthorized(new { message = "Không tìm thấy thông tin nhân viên trong token." });

                var reportId = await _shiftClosingService.SubmitShiftClosingReportAsync(staffId, dto);

                return Ok(new
                {
                    message = "Đã gửi báo cáo kết ca và đang chờ Quản lý duyệt.",
                    reportId
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi gửi báo cáo kết ca: " + ex.Message });
            }
        }

        [HttpGet("my-reports")]
        public async Task<IActionResult> GetMyReports()
        {
            try
            {
                var staffId = GetCurrentUserId();

                if (staffId <= 0)
                    return Unauthorized(new { message = "Không tìm thấy thông tin nhân viên trong token." });

                var data = await _shiftClosingService.GetMyReportsAsync(staffId);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi lấy lịch sử kết ca: " + ex.Message });
            }
        }

        [HttpGet("my-reports/{id:int}")]
        public async Task<IActionResult> GetMyReportDetail(int id)
        {
            try
            {
                var staffId = GetCurrentUserId();

                if (staffId <= 0)
                    return Unauthorized(new { message = "Không tìm thấy thông tin nhân viên trong token." });

                var data = await _shiftClosingService.GetMyReportDetailAsync(staffId, id);

                if (data == null)
                    return NotFound(new { message = "Không tìm thấy báo cáo kết ca." });

                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi lấy chi tiết báo cáo kết ca: " + ex.Message });
            }
        }

        [HttpGet("reports")]
        public async Task<IActionResult> GetReportsForManagement([FromQuery] int? branchId)
        {
            try
            {
                var finalBranchId = ResolveBranchIdForManagement(branchId);

                if (finalBranchId == -2)
                    return Forbid();

                if (finalBranchId == -1)
                    return Unauthorized(new { message = "Không tìm thấy thông tin cơ sở trong token." });

                var data = await _shiftClosingService.GetReportsForManagementAsync(
                    finalBranchId == 0 ? null : finalBranchId
                );

                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi lấy báo cáo kết ca: " + ex.Message });
            }
        }

        [HttpGet("reports/{id:int}")]
        public async Task<IActionResult> GetReportDetailForManagement(int id, [FromQuery] int? branchId)
        {
            try
            {
                var finalBranchId = ResolveBranchIdForManagement(branchId);

                if (finalBranchId == -2)
                    return Forbid();

                if (finalBranchId == -1)
                    return Unauthorized(new { message = "Không tìm thấy thông tin cơ sở trong token." });

                var data = await _shiftClosingService.GetReportDetailForManagementAsync(
                    id,
                    finalBranchId == 0 ? null : finalBranchId
                );

                if (data == null)
                    return NotFound(new { message = "Không tìm thấy báo cáo kết ca." });

                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi lấy chi tiết báo cáo kết ca: " + ex.Message });
            }
        }

        [HttpPut("reports/{id:int}/approve")]
        public async Task<IActionResult> ApproveReport(int id)
        {
            try
            {
                if (!IsCurrentUserManager())
                    return Forbid();

                var managerId = GetCurrentUserId();

                if (managerId <= 0)
                    return Unauthorized(new { message = "Không tìm thấy thông tin Quản lý trong token." });

                await _shiftClosingService.ApproveReportAsync(managerId, id);

                return Ok(new
                {
                    message = "Duyệt báo cáo thành công. Tồn quầy đã được cập nhật."
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi duyệt báo cáo: " + ex.Message });
            }
        }

        [HttpPut("reports/{id:int}/reject")]
        public async Task<IActionResult> RejectReport(int id, [FromBody] RejectShiftClosingDto dto)
        {
            try
            {
                if (!IsCurrentUserManager())
                    return Forbid();

                var managerId = GetCurrentUserId();

                if (managerId <= 0)
                    return Unauthorized(new { message = "Không tìm thấy thông tin Quản lý trong token." });

                await _shiftClosingService.RejectReportAsync(managerId, id, dto.Reason);

                return Ok(new
                {
                    message = "Đã từ chối báo cáo. Tồn quầy được giữ nguyên."
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi từ chối báo cáo: " + ex.Message });
            }
        }

        private int ResolveBranchIdForManagement(int? requestedBranchId)
        {
            var role = GetClaimValue(ClaimTypes.Role, "role", "Role")?.ToUpperInvariant();

            var isAdmin = role == "ADMIN" || role == "QUẢN TRỊ" || role == "QUAN TRI";
            var isManager = role == "MANAGER" || role == "QUẢN LÝ" || role == "QUAN LY";

            if (isAdmin)
            {
                return requestedBranchId.HasValue && requestedBranchId.Value > 0
                    ? requestedBranchId.Value
                    : 0;
            }

            if (isManager)
            {
                var branchIdStr = GetClaimValue("BranchId", "branchId", "branch_id");

                if (!int.TryParse(branchIdStr, out var branchId) || branchId <= 0)
                    return -1;

                return branchId;
            }

            return -2;
        }

        private bool IsCurrentUserManager()
        {
            var role = GetClaimValue(ClaimTypes.Role, "role", "Role")?.ToUpperInvariant();

            return role == "MANAGER"
                || role == "QUẢN LÝ"
                || role == "QUAN LY";
        }

        private int GetCurrentUserId()
        {
            var value = GetClaimValue(
                ClaimTypes.NameIdentifier,
                "UserId",
                "userId",
                "id",
                "Id"
            );

            return int.TryParse(value, out var userId) ? userId : 0;
        }

        private string? GetClaimValue(params string[] claimTypes)
        {
            foreach (var claimType in claimTypes)
            {
                var value = User.Claims.FirstOrDefault(c => c.Type == claimType)?.Value;

                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }

            return null;
        }
    }
}

using System.Security.Claims;
using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LuanVanTotNghiep.Controllers;

/// <summary>
/// Controller chỉ phụ trách CHẤM CÔNG.
///
/// Nghiệp vụ thật vẫn nằm trong AttendanceService.
/// Controller chỉ:
/// - Nhận request.
/// - Lấy ManagerId từ JWT.
/// - Gọi Service.
/// - Trả HTTP response.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "MANAGER")]
public class AttendanceController : ControllerBase
{
    private readonly AttendanceService
        _attendanceService;

    public AttendanceController(
        AttendanceService attendanceService)
    {
        _attendanceService =
            attendanceService;
    }

    /// <summary>
    /// Không tin ManagerId do Frontend gửi lên.
    /// ManagerId luôn được lấy từ JWT.
    /// </summary>
    private int GetCurrentUserId()
    {
        var userIdValue =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!int.TryParse(
                userIdValue,
                out var userId) ||
            userId <= 0)
        {
            throw new UnauthorizedAccessException(
                "Không xác định được người dùng hiện tại.");
        }

        return userId;
    }

    /// <summary>
    /// Manager quét QR để CHECKIN hoặc CHECKOUT.
    ///
    /// Route mới:
    /// POST /api/Attendance/scan
    ///
    /// Route cũ được giữ tạm để màn hình cũ vẫn chạy:
    /// POST /api/StaffRegistration/scan-attendance
    /// </summary>
    [HttpPost("scan")]
    [HttpPost("~/api/StaffRegistration/scan-attendance")]
    public async Task<IActionResult> ScanAttendance(
        [FromBody] ScanAttendanceDto dto)
    {
        try
        {
            // Ghi đè ManagerId bằng JWT.
            dto.ManagerId =
                GetCurrentUserId();

            var result =
                await _attendanceService
                    .ScanAttendanceAsync(dto);

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new
            {
                message = ex.Message
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                message = ex.Message
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }
}
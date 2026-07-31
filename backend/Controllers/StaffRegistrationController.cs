using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LuanVanTotNghiep.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StaffRegistrationController : ControllerBase
{
    private readonly StaffRegistrationService _registrationService;
    private readonly FinalScheduleService _finalScheduleService;
    private readonly AttendanceService _attendanceService;

    public StaffRegistrationController(
        StaffRegistrationService registrationService,
        FinalScheduleService finalScheduleService,
        AttendanceService attendanceService)
    {
        _registrationService = registrationService;
        _finalScheduleService = finalScheduleService;
        _attendanceService = attendanceService;
    }

    // Nhân viên đăng ký ca
    [HttpPost]
    public async Task<IActionResult> Register(
        [FromBody] RegisterShiftDto dto)
    {
        try
        {
            await _registrationService.RegisterAsync(dto);

            return Ok(new
            {
                message = "Đăng ký ca làm thành công!"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Lấy danh sách đăng ký của một đợt
    [HttpGet("period/{periodId:int}")]
    public async Task<IActionResult> GetByPeriod(int periodId)
    {
        var list = await _registrationService
            .GetRegistrationsByPeriodAsync(periodId);

        return Ok(list);
    }

    // Cập nhật trạng thái đăng ký
    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(
        int id,
        [FromBody] string newStatus)
    {
        try
        {
            await _registrationService.UpdateStatusAsync(
                id,
                newStatus);

            return Ok(new
            {
                message =
                    $"Đã chuyển trạng thái thành: {newStatus}"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Manager công bố lịch làm chính thức
    [HttpPost("publish")]
    [Authorize(Roles = "MANAGER")]
    public async Task<IActionResult> PublishSchedule(
        [FromBody] PublishScheduleDto dto)
    {
        try
        {
            await _finalScheduleService
                .PublishScheduleAsync(dto);

            return Ok(new
            {
                message =
                    "Đã công bố lịch làm việc thành công!"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Manager quét QR điểm danh vào hoặc ra ca
    [HttpPost("scan-attendance")]
    public async Task<IActionResult> ScanAttendance(
        [FromBody] ScanAttendanceDto dto)
    {
        try
        {
            var actorIdValue =
                User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(actorIdValue, out var actorId) ||
                actorId <= 0)
            {
                return Unauthorized(new
                {
                    message = "Không xác định được người thao tác."
                });
            }

            // Không tin ManagerId do client gửi lên.
            dto.ManagerId = actorId;
            var result = await _attendanceService
                .ScanAttendanceAsync(dto);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Nhân viên xem các ca đã đăng ký
    [HttpGet("my-schedule/{userId:int}/{periodId:int}")]
    public async Task<IActionResult> GetMySchedule(
        int userId,
        int periodId)
    {
        var schedule = await _registrationService
            .GetMyScheduleAsync(userId, periodId);

        return Ok(schedule);
    }

    // Nhân viên hủy ca đã đăng ký
    [HttpDelete("{id:int}/user/{userId:int}")]
    public async Task<IActionResult> CancelRegistration(
        int id,
        int userId)
    {
        try
        {
            await _registrationService
                .CancelRegistrationAsync(id, userId);

            return Ok(new
            {
                message = "Hủy ca thành công."
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Lấy lịch làm chính thức của một đợt
    [HttpGet("final-schedule/period/{periodId:int}")]
    public async Task<IActionResult> GetFinalScheduleByPeriod(
        int periodId)
    {
        try
        {
            var result = await _finalScheduleService
                .GetFinalSchedulesByPeriodAsync(periodId);

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

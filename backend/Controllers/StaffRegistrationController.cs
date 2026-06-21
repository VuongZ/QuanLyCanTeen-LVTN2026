using Microsoft.AspNetCore.Mvc;
using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Services;

namespace LuanVanTotNghiep.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StaffRegistrationController : ControllerBase
{
    private readonly StaffRegistrationService _service;

    public StaffRegistrationController(StaffRegistrationService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterShiftDto dto)
    {
        try
        {
            await _service.RegisterAsync(dto);
            return Ok(new { message = "Đăng ký ca làm thành công! Vui lòng đợi quản lý duyệt." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Lấy danh sách để Manager xem
    [HttpGet("period/{periodId}")]
    public async Task<IActionResult> GetByPeriod(int periodId)
    {
        var list = await _service.GetRegistrationsByPeriodAsync(periodId);
        return Ok(list);
    }

    // Manager bấm nút duyệt/từ chối (Gửi text thuần như "Đã Duyệt" hoặc "Từ Chối")
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] string newStatus)
    {
        try
        {
            await _service.UpdateStatusAsync(id, newStatus);
            return Ok(new { message = $"Đã chuyển trạng thái thành: {newStatus}" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Manager bấm chốt toàn bộ lịch làm của 1 tuần
    [HttpPost("publish")]
    public async Task<IActionResult> PublishSchedule([FromBody] PublishScheduleDto dto)
    {
        try
        {
            await _service.PublishScheduleAsync(dto);
            return Ok(new { message = "Đã chốt lịch làm việc thành công! Nhân viên giờ có thể xem lịch chính thức." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Nhân viên xem lịch cá nhân trong 1 đợt
    [HttpGet("my-schedule/{userId}/{periodId}")]
    public async Task<IActionResult> GetMySchedule(int userId, int periodId)
    {
        var schedule = await _service.GetMyScheduleAsync(userId, periodId);
        return Ok(schedule);
    }

    // Nhân viên hủy ca
    [HttpDelete("{id}/user/{userId}")]
    public async Task<IActionResult> CancelRegistration(int id, int userId)
    {
        try
        {
            await _service.CancelRegistrationAsync(id, userId);
            return Ok(new { message = "Hủy ca thành công." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
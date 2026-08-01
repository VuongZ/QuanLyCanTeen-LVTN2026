using System.Security.Claims;
using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LuanVanTotNghiep.Controllers;

[ApiController]
[Route("api/supplemental-attendance")]
[Authorize]
public class SupplementalAttendanceController : ControllerBase
{
    private readonly SupplementalAttendanceService _service;

    public SupplementalAttendanceController(SupplementalAttendanceService service) => _service = service;

    [HttpGet("candidates")]
    public async Task<IActionResult> GetCandidates([FromQuery] DateOnly workDate) =>
        await ExecuteAsync(() => _service.GetCandidatesAsync(GetCurrentUserId(), workDate));

    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] SubmitSupplementalAttendanceDto dto)
    {
        try
        {
            await _service.SubmitAsync(GetCurrentUserId(), dto);
            return Ok(new { message = "Đã gửi yêu cầu chấm công bổ sung cho admin duyệt." });
        }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("mine")]
    public async Task<IActionResult> GetMine() =>
        await ExecuteAsync(() => _service.GetMineAsync(GetCurrentUserId()));

    [HttpGet("review")]
    public async Task<IActionResult> GetForReview() =>
        await ExecuteAsync(() => _service.GetForReviewAsync(GetCurrentUserId()));

    [HttpPut("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        try
        {
            await _service.ApproveAsync(GetCurrentUserId(), id);
            return Ok(new { message = "Đã duyệt chấm công bổ sung và cộng giờ vào lương ngày làm." });
        }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("{id:int}/reject")]
    public async Task<IActionResult> Reject(int id, [FromBody] RejectSupplementalAttendanceDto dto)
    {
        try
        {
            await _service.RejectAsync(GetCurrentUserId(), id, dto.Reason);
            return Ok(new { message = "Đã từ chối yêu cầu chấm công bổ sung." });
        }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    private async Task<IActionResult> ExecuteAsync<T>(Func<Task<T>> action)
    {
        try { return Ok(await action()); }
        catch (UnauthorizedAccessException ex) { return StatusCode(403, new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    private int GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("UserId") ?? User.FindFirstValue("userId") ?? User.FindFirstValue("id");
        if (!int.TryParse(value, out var id) || id <= 0)
            throw new UnauthorizedAccessException("Không tìm thấy người dùng trong token.");
        return id;
    }
}

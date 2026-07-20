using System.Security.Claims;
using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LuanVanTotNghiep.Controllers;

[ApiController]
[Route("api/checkout-requests")]
[Authorize]
public class CheckoutRequestController : ControllerBase
{
    private readonly CheckoutRequestService _service;

    public CheckoutRequestController(CheckoutRequestService service) => _service = service;

    [HttpGet("mine")]
    public async Task<IActionResult> GetMine()
    {
        var userId = GetCurrentUserId();
        if (userId <= 0) return Unauthorized(new { message = "Không tìm thấy người dùng trong token." });
        return Ok(await _service.GetMineAsync(userId));
    }

    [HttpPut("{id:int}/submit")]
    public async Task<IActionResult> Submit(int id, [FromBody] SubmitCheckoutRequestDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId <= 0) return Unauthorized(new { message = "Không tìm thấy người dùng trong token." });
            await _service.SubmitAsync(userId, id, dto);
            return Ok(new { message = "Đã gửi giờ checkout và đang chờ duyệt." });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("review")]
    public async Task<IActionResult> GetForReview()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId <= 0) return Unauthorized(new { message = "Không tìm thấy người dùng trong token." });
            return Ok(await _service.GetForReviewAsync(userId));
        }
        catch (InvalidOperationException ex) { return StatusCode(403, new { message = ex.Message }); }
    }

    [HttpPut("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId <= 0) return Unauthorized(new { message = "Không tìm thấy người dùng trong token." });
            await _service.ApproveAsync(userId, id);
            return Ok(new { message = "Đã duyệt checkout và cập nhật bảng lương." });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("{id:int}/reject")]
    public async Task<IActionResult> Reject(int id, [FromBody] RejectCheckoutRequestDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId <= 0) return Unauthorized(new { message = "Không tìm thấy người dùng trong token." });
            await _service.RejectAsync(userId, id, dto.Reason);
            return Ok(new { message = "Đã từ chối. Người yêu cầu có thể chỉnh sửa và gửi lại." });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    private int GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("UserId") ?? User.FindFirstValue("userId")
            ?? User.FindFirstValue("id");
        return int.TryParse(value, out var id) ? id : 0;
    }
}

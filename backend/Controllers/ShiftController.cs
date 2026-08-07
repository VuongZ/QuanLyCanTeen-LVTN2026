using System.Security.Claims;
using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LuanVanTotNghiep.DTOs;

namespace LuanVanTotNghiep.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ShiftController : ControllerBase
{
    private readonly ShiftService _shiftService;

    public ShiftController(ShiftService shiftService)
    {
        _shiftService = shiftService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool includeInactive = false)
    {
        var canSeeInactive = includeInactive && User.IsInRole("ADMIN");
        var shifts = await _shiftService.GetAllShiftsAsync(canSeeInactive);
        return Ok(shifts);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var shift = await _shiftService.GetShiftByIdAsync(id);
        if (shift == null)
        {
            return NotFound(new { message = "Không tìm thấy ca làm!" });
        }

        return Ok(shift);
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> CreateShift([FromBody] CreateShiftDto dto)
    {
        try
        {
            var result = await _shiftService.CreateShiftWithAutoConfigAsync(dto);
            return Ok(new
            {
                message = "Tạo ca làm và tự động cấu hình 7 ngày thành công!",
                data = result
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi khi tạo ca: " + ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] CaShift shift)
    {
        try
        {
            var updated = await _shiftService.UpdateShiftAsync(id, shift);
            return Ok(new
            {
                message = "Đã cập nhật ca làm thành công!",
                data = updated
            });
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

    [HttpPatch("{id:int}/deactivate")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Deactivate(
        int id,
        [FromBody] ChangeShiftStatusDto? dto)
    {
        try
        {
            var updated = await _shiftService.DeactivateShiftAsync(
                id,
                GetCurrentUserId(),
                dto?.Reason);

            return Ok(new
            {
                message = "Đã ngừng hoạt động ca làm. Lịch sử đã công bố vẫn được giữ nguyên.",
                data = updated
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:int}/restore")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Restore(int id)
    {
        try
        {
            var updated = await _shiftService.RestoreShiftAsync(id);
            return Ok(new
            {
                message = "Đã khôi phục ca làm.",
                data = updated
            });
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

    private int GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : 0;
    }
}

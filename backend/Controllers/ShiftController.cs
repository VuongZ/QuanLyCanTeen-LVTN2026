using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.Services;
using Microsoft.AspNetCore.Mvc;
using LuanVanTotNghiep.DTOs;

namespace LuanVanTotNghiep.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShiftController : ControllerBase
{
    private readonly ShiftService _shiftService;

    public ShiftController(ShiftService shiftService)
    {
        _shiftService = shiftService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var shifts = await _shiftService.GetAllShiftsAsync();
        return Ok(shifts);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var shift = await _shiftService.GetShiftByIdAsync(id);
        if (shift == null) return NotFound(new { message = "Không tìm thấy ca làm!" });
        
        return Ok(shift);
    }

    // POST MỚI: Gọi thẳng vào hàm tạo Ca kèm 7 ngày Config
    [HttpPost]
    public async Task<IActionResult> CreateShift([FromBody] CreateShiftDto dto)
    {
        try
        {
            var result = await _shiftService.CreateShiftWithAutoConfigAsync(dto);
            return Ok(new { message = "Tạo ca làm và tự động cấu hình 7 ngày thành công!", data = result });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi khi tạo ca: " + ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CaShift shift)
    {
        if (id != shift.Id) return BadRequest(new { message = "ID không khớp!" });
        
        await _shiftService.UpdateShiftAsync(shift);
        return Ok(new { message = "Đã cập nhật ca làm thành công!" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _shiftService.DeleteShiftAsync(id);
        return Ok(new { message = "Đã xóa ca làm thành công!" });
    }
}
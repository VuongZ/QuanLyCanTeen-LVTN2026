using LuanVanTotNghiep.Models.Entities;
using LuanVanTotNghiep.Services;
using Microsoft.AspNetCore.Mvc;

namespace LuanVanTotNghiep.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShiftController(ShiftService shiftService) : ControllerBase
{
    // GET: Lấy danh sách tất cả các ca làm (Sử dụng DTO để hiển thị gọn gàng)
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var shifts = await shiftService.GetAllShiftsAsync();
        return Ok(shifts);
    }

    // GET: Lấy 1 ca làm theo ID
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var shift = await shiftService.GetShiftByIdAsync(id);
        if (shift == null) return NotFound(new { message = "Không tìm thấy ca làm!" });
        
        return Ok(shift);
    }

    // POST: Thêm ca làm mới
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CaShift shift)
    {
        await shiftService.AddShiftAsync(shift);
        return Ok(new { message = "Đã thêm ca làm thành công!" });
    }

    // PUT: Sửa thông tin ca làm
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CaShift shift)
    {
        if (id != shift.Id) return BadRequest(new { message = "ID không khớp!" });
        
        await shiftService.UpdateShiftAsync(shift);
        return Ok(new { message = "Đã cập nhật ca làm thành công!" });
    }

    // DELETE: Xóa ca làm
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await shiftService.DeleteShiftAsync(id);
        return Ok(new { message = "Đã xóa ca làm thành công!" });
    }
}
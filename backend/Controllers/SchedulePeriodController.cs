using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.Services;
using LuanVanTotNghiep.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization; // 👉 Bổ sung thư viện này

namespace LuanVanTotNghiep.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // 👈 Ổ KHÓA LỚP 1: Chặn tất cả những ai chưa đăng nhập
public class SchedulePeriodController(SchedulePeriodService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var periods = await service.GetAllAsync();
        return Ok(periods);
    }

    // API cực kỳ quan trọng cho React: Gọi lấy các đợt đang OPEN
    [HttpGet("open")]
    public async Task<IActionResult> GetOpenPeriods()
    {
        var periods = await service.GetOpenPeriodsAsync();
        return Ok(periods);
    }

    [HttpPost]
    [Authorize(Roles = "MANAGER")] // 👈 Ổ KHÓA LỚP 2: Tránh Staff tự tạo đợt
    public async Task<IActionResult> Create([FromBody] CreatePeriodDto dto)
    {
        try
        {
            await service.AddAsync(dto);
            return Ok(new { message = "Đã tạo đợt đăng ký mới!" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

   [HttpPut("{id}")]
   [Authorize(Roles = "MANAGER")] // 👈 Ổ KHÓA LỚP 2: Tránh Staff tự sửa đợt
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePeriodDto dto)
    {
        try
        {
            await service.UpdateAsync(id, dto);
            return Ok(new { message = "Cập nhật thành công!" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "MANAGER")] 
    public async Task<IActionResult> Delete(int id)
    {
        await service.DeleteAsync(id);
        return Ok(new { message = "Xóa đợt đăng ký thành công!" });
    }

    [HttpPatch("{id}/status")]
[Authorize(Roles = "MANAGER")]
public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto dto)
{
    try
    {
        await service.UpdateStatusOnlyAsync(id, dto.Status);
        return Ok(new { message = "Cập nhật trạng thái thành công!" });
    }
    catch (Exception ex)
    {
        return BadRequest(new { message = ex.Message });
    }
}
}
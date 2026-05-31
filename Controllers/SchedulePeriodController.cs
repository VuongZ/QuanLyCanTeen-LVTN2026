using LuanVanTotNghiep.Models.Entities;
using LuanVanTotNghiep.Services;
using Microsoft.AspNetCore.Mvc;

namespace LuanVanTotNghiep.Controllers;

[ApiController]
[Route("api/[controller]")]
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
    public async Task<IActionResult> Create([FromBody] CaSchedulePeriod period)
    {
        await service.AddAsync(period);
        return Ok(new { message = "Đã tạo đợt đăng ký mới!" });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CaSchedulePeriod period)
    {
        if (id != period.Id) return BadRequest(new { message = "ID không khớp!" });
        await service.UpdateAsync(period);
        return Ok(new { message = "Cập nhật thành công!" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await service.DeleteAsync(id);
        return Ok(new { message = "Xóa đợt đăng ký thành công!" });
    }
}
using LuanVanTotNghiep.Models.Entities;
using LuanVanTotNghiep.Services;
using Microsoft.AspNetCore.Mvc;

namespace LuanVanTotNghiep.Controllers;

[ApiController]
[Route("api/[controller]")]
// Đã đổi tên Class và Inject đúng BranchShiftConfigService
public class BranchShiftConfigController(BranchShiftConfigService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var configs = await service.GetAllAsync();
        return Ok(configs);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CaBranchShiftConfig config)
    {
        await service.AddAsync(config);
        return Ok(new { message = "Đã tạo cấu hình định mức ca thành công!" });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CaBranchShiftConfig config)
    {
        if (id != config.Id) return BadRequest(new { message = "ID không khớp!" });
        await service.UpdateAsync(config);
        return Ok(new { message = "Cập nhật cấu hình thành công!" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await service.DeleteAsync(id);
        return Ok(new { message = "Xóa cấu hình thành công!" });
    }
}
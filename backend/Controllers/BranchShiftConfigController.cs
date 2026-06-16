using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Services;
using Microsoft.AspNetCore.Mvc;

namespace LuanVanTotNghiep.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BranchShiftConfigController : ControllerBase
{
    private readonly BranchShiftConfigService _service;

    public BranchShiftConfigController(BranchShiftConfigService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var configs = await _service.GetAllAsync();
        return Ok(configs);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SaveShiftConfigDto dto)
    {
        try
        {
            await _service.AddAsync(dto);
            return Ok(new { message = "Đã tạo cấu hình ca thành công!" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] SaveShiftConfigDto dto)
    {
        try
        {
            await _service.UpdateAsync(id, dto);
            return Ok(new { message = "Cập nhật cấu hình thành công!" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _service.DeleteAsync(id);
            return Ok(new { message = "Xóa cấu hình thành công!" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Lỗi khi xóa: " + ex.Message });
        }
    }
}
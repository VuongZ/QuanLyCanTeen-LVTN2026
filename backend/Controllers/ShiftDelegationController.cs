using System.Security.Claims;
using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LuanVanTotNghiep.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ShiftDelegationController(ShiftDelegationService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetVisible([FromQuery] int? branchId)
    {
        try { return Ok(await service.GetVisibleAsync(GetCurrentUserId(), branchId)); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN,MANAGER")]
    public async Task<IActionResult> Create([FromBody] CreateShiftDelegationDto dto)
    {
        try { return Ok(await service.CreateAsync(GetCurrentUserId(), dto)); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("{id:int}/respond")]
    [Authorize(Roles = "STAFF")]
    public async Task<IActionResult> Respond(int id, [FromBody] RespondShiftDelegationDto dto)
    {
        try { return Ok(await service.RespondAsync(GetCurrentUserId(), id, dto.Accept)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("{id:int}/revoke")]
    [Authorize(Roles = "ADMIN,MANAGER")]
    public async Task<IActionResult> Revoke(int id)
    {
        try { return Ok(await service.RevokeAsync(GetCurrentUserId(), id)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost("attendance-status")]
    public async Task<IActionResult> MarkAttendanceStatus(
        [FromBody] MarkDelegatedAttendanceDto dto)
    {
        try { return Ok(await service.MarkAttendanceStatusAsync(GetCurrentUserId(), dto)); }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    private int GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var id) ? id : 0;
    }
}

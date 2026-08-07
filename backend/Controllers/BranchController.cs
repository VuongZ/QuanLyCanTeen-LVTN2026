using System.Security.Claims;
using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LuanVanTotNghiep.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BranchController : ControllerBase
{
    private readonly BranchService _service;

    public BranchController(BranchService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool includeInactive = false)
    {
        var canSeeInactive = includeInactive && User.IsInRole("ADMIN");
        var branches = await _service.GetAllBranchAsync(canSeeInactive);
        return Ok(branches);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var branch = await _service.GetBranchByIdAsync(id);
        if (branch == null)
        {
            return NotFound(new { message = "Không tìm thấy cơ sở." });
        }

        return Ok(branch);
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Create([FromBody] DmBranch branch)
    {
        try
        {
            var created = await _service.AddBranchAsync(branch);
            return Ok(created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] DmBranch branchInput)
    {
        try
        {
            var updated = await _service.UpdateBranchAsync(id, branchInput);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:int}/deactivate")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Deactivate(
        int id,
        [FromBody] ChangeBranchStatusDto? dto)
    {
        try
        {
            var updated = await _service.DeactivateBranchAsync(
                id,
                GetCurrentUserId(),
                dto?.Reason);

            return Ok(new
            {
                message = "Đã ngừng hoạt động cơ sở.",
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
            var updated = await _service.RestoreBranchAsync(id);
            return Ok(new
            {
                message = "Đã khôi phục hoạt động cơ sở.",
                data = updated
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    private int GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : 0;
    }
}

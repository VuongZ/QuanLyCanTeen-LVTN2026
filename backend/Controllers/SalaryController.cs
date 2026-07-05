using System.Security.Claims;
using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SalaryController : ControllerBase
{
    private readonly SalaryService _salaryService;
    private readonly AppDbContext _context;

    public SalaryController(SalaryService salaryService, AppDbContext context)
    {
        _salaryService = salaryService;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        if (!IsAdmin())
            return Forbid();

        var salaries = await _salaryService.GetAllAsync();
        return Ok(salaries);
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUser(int userId)
    {
        var role = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value?.ToUpperInvariant();
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        var currentUser = int.TryParse(userIdClaim, out var currentUserId)
            ? await _context.NsUsers.AsNoTracking().FirstOrDefaultAsync(u => u.Id == currentUserId)
            : null;

        if (currentUser == null)
            return Unauthorized(new { message = "Khong xac dinh duoc nguoi dung." });

        if (role != "ADMIN" && role != "MANAGER" && currentUser.Id != userId)
            return Forbid();

        var salaries = await _salaryService.GetByUserAsync(userId);
        return Ok(salaries);
    }

    [HttpGet("rule-adjustments")]
    public async Task<IActionResult> GetRuleAdjustments([FromQuery] int month, [FromQuery] int year, [FromQuery] int? branchId)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null)
            return Unauthorized(new { message = "Khong xac dinh duoc nguoi dung." });

        if (!IsAdmin() && !IsManager())
            return Forbid();

        var resolvedBranch = ResolveBranchId(currentUser, branchId);
        if (resolvedBranch == null)
            return BadRequest(new { message = "Vui long chon co so." });

        var result = await _salaryService.GetRuleAdjustmentsAsync(resolvedBranch.Value, month, year);
        return Ok(result);
    }

    [HttpPut("rule")]
    public async Task<IActionResult> UpdateSalaryRule([FromBody] UpdateSalaryRuleDto dto)
    {
        if (!IsAdmin())
            return Forbid();

        try
        {
            var result = await _salaryService.UpsertSalaryRuleAsync(dto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("rule-adjustments/apply")]
    public async Task<IActionResult> ApplyRuleAdjustment([FromBody] ApplySalaryRuleDto dto, [FromQuery] int? branchId)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null)
            return Unauthorized(new { message = "Khong xac dinh duoc nguoi dung." });

        if (!IsAdmin() && !IsManager())
            return Forbid();

        var resolvedBranch = ResolveBranchId(currentUser, branchId);
        if (resolvedBranch == null)
            return BadRequest(new { message = "Vui long chon co so." });

        try
        {
            var result = await _salaryService.ApplyRuleAdjustmentAsync(resolvedBranch.Value, dto);
            if (result == null)
                return NotFound(new { message = "Khong tim thay nhan vien trong co so cua ban." });

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("rule-adjustments/manual")]
    public async Task<IActionResult> AddManualAdjustment([FromBody] ManualSalaryAdjustmentDto dto, [FromQuery] int? branchId)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null)
            return Unauthorized(new { message = "Khong xac dinh duoc nguoi dung." });

        if (!IsAdmin() && !IsManager())
            return Forbid();

        var resolvedBranch = ResolveBranchId(currentUser, branchId);
        if (resolvedBranch == null)
            return BadRequest(new { message = "Vui long chon co so." });

        try
        {
            var result = await _salaryService.AddManualAdjustmentAsync(resolvedBranch.Value, dto);
            if (result == null)
                return NotFound(new { message = "Khong tim thay nhan vien trong co so cua ban." });

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{salaryId}/pay")]
    public async Task<IActionResult> MarkPaid(int salaryId)
    {
        if (!IsAdmin())
            return Forbid();

        var salary = await _salaryService.MarkPaidAsync(salaryId);
        if (salary == null)
            return NotFound(new { message = "Khong tim thay bang luong." });

        return Ok(salary);
    }

    private bool IsAdmin()
    {
        var role = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
        return string.Equals(role, "ADMIN", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsManager()
    {
        var role = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
        return string.Equals(role, "MANAGER", StringComparison.OrdinalIgnoreCase);
    }

    private int? ResolveBranchId(NsUser currentUser, int? requestedBranchId)
    {
        if (IsAdmin())
            return requestedBranchId ?? currentUser.BranchId;

        if (currentUser.BranchId == null)
            return null;

        if (requestedBranchId != null && requestedBranchId.Value != currentUser.BranchId.Value)
            return null;

        return currentUser.BranchId.Value;
    }

    private async Task<NsUser?> GetCurrentUserAsync()
    {
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var currentUserId)
            ? await _context.NsUsers.AsNoTracking().FirstOrDefaultAsync(u => u.Id == currentUserId)
            : null;
    }
}

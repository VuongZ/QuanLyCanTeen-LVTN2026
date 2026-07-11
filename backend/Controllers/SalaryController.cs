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

        var summaries = await _salaryService.GetBranchSummariesAsync();
        return Ok(summaries);
    }

    [HttpGet("branch")]
    public async Task<IActionResult> GetBranchSalaries()
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null)
            return Unauthorized(new { message = "Không Xác Định Được Người Dùng" });

        if (!IsManager())
            return Forbid();

        if (currentUser.BranchId == null)
            return BadRequest(new { message = "Tài Khoản Quản Lý Chưa Được Gắn Cơ Sở" });

        var salaries = await _salaryService.GetByBranchAsync(currentUser.BranchId.Value);
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
            return Unauthorized(new { message = "Không Xác Định Được Người Dùng." });

        if (currentUser.Id != userId && role != "MANAGER")
            return Forbid();

        if (role == "MANAGER")
        {
            if (currentUser.BranchId == null)
                return BadRequest(new { message = "Tài Khoản Quản Lý Chưa Được Gắn Cơ Sở." });

            var targetBranchId = await _context.NsUsers
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => u.BranchId)
                .FirstOrDefaultAsync();

            if (targetBranchId != currentUser.BranchId)
                return Forbid();
        }

        var salaries = await _salaryService.GetByUserAsync(userId);
        return Ok(salaries);
    }

    [HttpGet("rule-adjustments")]
    public async Task<IActionResult> GetRuleAdjustments([FromQuery] int month, [FromQuery] int year, [FromQuery] int? branchId)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null)
            return Unauthorized(new { message = "Không Xác Địng Được Người Dùng." });

        if (!IsAdminOrManager())
            return Forbid();

        var resolvedBranch = ResolveBranchId(currentUser, branchId);
        if (resolvedBranch == null)
            return BadRequest(new { message = "Vui lòng chọn cơ sở." });

        var result = await _salaryService.GetRuleAdjustmentsAsync(resolvedBranch.Value, month, year);
        return Ok(result);
    }

    [HttpPut("rule")]
    public async Task<IActionResult> UpdateSalaryRule([FromBody] UpdateSalaryRuleDto dto)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null)
            return Unauthorized(new { message = "Không Xác Định Được Người Dùng." });

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
            return Unauthorized(new { message = "Không Xác Định Được Người Dùng." });

        if (!IsManager())
            return Forbid();

        var resolvedBranch = ResolveBranchId(currentUser, branchId);
        if (resolvedBranch == null)
            return BadRequest(new { message = "Vui lòng chọn cơ sở." });

        try
        {
            var result = await _salaryService.ApplyRuleAdjustmentAsync(resolvedBranch.Value, dto);
            if (result == null)
                return NotFound(new { message = "Không Tìm Thấy Nhân Viên Trong Cơ Sở Của Bạn." });

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
            return Unauthorized(new { message = "Không Xác Định Được Người Dùng." });

        if (!IsManager())
            return Forbid();

        var resolvedBranch = ResolveBranchId(currentUser, branchId);
        if (resolvedBranch == null)
            return BadRequest(new { message = "Vui lòng chọn cơ sở." });

        try
        {
            var result = await _salaryService.AddManualAdjustmentAsync(resolvedBranch.Value, dto);
            if (result == null)
                return NotFound(new { message = "Không Tìm Thấy Nhân Viên Trong Cơ Sở Của Bạn." });

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
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null)
            return Unauthorized(new { message = "Không Xác Định Được Người Dùng." });

        if (!IsManager())
            return Forbid();

        if (currentUser.BranchId == null)
            return BadRequest(new { message = "Tài Khoản Quản Lý Chưa Được Gắn cơ sở." });

        var salary = await _salaryService.MarkPaidAsync(salaryId, currentUser.BranchId.Value);
        if (salary == null)
            return NotFound(new { message = "Không Tìm Thấy Bảng Lương Trong Cơ Sở Của Bạn." });

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

    private bool IsAdminOrManager()
    {
        return IsAdmin() || IsManager();
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

using System.Security.Claims;
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

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUser(int userId)
    {
        var role = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value?.ToUpperInvariant();
        var username = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
        var currentUser = await _context.NsUsers.AsNoTracking().FirstOrDefaultAsync(u => u.Username == username);

        if (currentUser == null)
            return Unauthorized(new { message = "Khong xac dinh duoc nguoi dung." });

        if (role != "ADMIN" && role != "MANAGER" && currentUser.Id != userId)
            return Forbid();

        var salaries = await _salaryService.GetByUserAsync(userId);
        return Ok(salaries);
    }
}

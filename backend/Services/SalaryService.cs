using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.backend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Services;

public class SalaryService
{
    private readonly AppDbContext _context;

    public SalaryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<SalaryDto>> GetByUserAsync(int userId)
    {
        return await _context.LuongMonthlySalaries
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.Year)
            .ThenByDescending(s => s.Month)
            .Select(s => new SalaryDto
            {
                Id = s.Id,
                UserId = s.UserId,
                Username = s.User.Username,
                FullName = s.User.FullName,
                BranchName = s.User.Branch != null ? s.User.Branch.Name : null,
                BankName = s.User.BankName,
                BankAccountNumber = s.User.BankAccountNumber,
                BankAccountName = s.User.BankAccountName,
                Month = s.Month,
                Year = s.Year,
                TotalHours = s.TotalHours,
                HourlyWageAtTime = s.HourlyWageAtTime,
                TotalSalary = s.TotalSalary,
                TotalBonus = s.TotalBonus ?? 0,
                TotalPenalty = s.TotalPenalty ?? 0,
                Status = s.Status,
                PaidAt = s.PaidAt,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<List<SalaryDto>> GetAllAsync()
    {
        return await _context.LuongMonthlySalaries
            .AsNoTracking()
            .Include(s => s.User)
            .ThenInclude(u => u.Branch)
            .OrderByDescending(s => s.Year)
            .ThenByDescending(s => s.Month)
            .ThenBy(s => s.User.FullName ?? s.User.Username)
            .Select(s => new SalaryDto
            {
                Id = s.Id,
                UserId = s.UserId,
                Username = s.User.Username,
                FullName = s.User.FullName,
                BranchName = s.User.Branch != null ? s.User.Branch.Name : null,
                BankName = s.User.BankName,
                BankAccountNumber = s.User.BankAccountNumber,
                BankAccountName = s.User.BankAccountName,
                Month = s.Month,
                Year = s.Year,
                TotalHours = s.TotalHours,
                HourlyWageAtTime = s.HourlyWageAtTime,
                TotalSalary = s.TotalSalary,
                TotalBonus = s.TotalBonus ?? 0,
                TotalPenalty = s.TotalPenalty ?? 0,
                Status = s.Status,
                PaidAt = s.PaidAt,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<SalaryDto?> MarkPaidAsync(int salaryId)
    {
        var salary = await _context.LuongMonthlySalaries
            .Include(s => s.User)
            .ThenInclude(u => u.Branch)
            .FirstOrDefaultAsync(s => s.Id == salaryId);

        if (salary == null)
            return null;

        salary.Status = "PAID";
        salary.PaidAt = DateTime.Now;
        await _context.SaveChangesAsync();

        return ToDto(salary);
    }

    private static SalaryDto ToDto(LuongMonthlySalary s)
    {
        return new SalaryDto
        {
            Id = s.Id,
            UserId = s.UserId,
            Username = s.User?.Username,
            FullName = s.User?.FullName,
            BranchName = s.User?.Branch?.Name,
            BankName = s.User?.BankName,
            BankAccountNumber = s.User?.BankAccountNumber,
            BankAccountName = s.User?.BankAccountName,
            Month = s.Month,
            Year = s.Year,
            TotalHours = s.TotalHours,
            HourlyWageAtTime = s.HourlyWageAtTime,
            TotalSalary = s.TotalSalary,
            TotalBonus = s.TotalBonus ?? 0,
            TotalPenalty = s.TotalPenalty ?? 0,
            Status = s.Status,
            PaidAt = s.PaidAt,
            CreatedAt = s.CreatedAt
        };
    }
}

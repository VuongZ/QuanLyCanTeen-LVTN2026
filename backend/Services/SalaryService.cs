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
                Username = s.User.Email ?? s.User.PhoneNumber,
                FullName = s.User.FullName,
                BranchName = s.User.Branch != null ? s.User.Branch.Name : null,
                BankName = s.User.NsUserBankAccounts.Select(b => b.BankName).FirstOrDefault(),
                BankAccountNumber = s.User.NsUserBankAccounts.Select(b => b.BankAccountNumber).FirstOrDefault(),
                BankAccountName = s.User.NsUserBankAccounts.Select(b => b.BankAccountName).FirstOrDefault(),
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
            .Include(s => s.User)
            .ThenInclude(u => u.NsUserBankAccounts)
            .OrderByDescending(s => s.Year)
            .ThenByDescending(s => s.Month)
            .ThenBy(s => s.User.FullName ?? s.User.Email ?? s.User.PhoneNumber)
            .Select(s => new SalaryDto
            {
                Id = s.Id,
                UserId = s.UserId,
                Username = s.User.Email ?? s.User.PhoneNumber,
                FullName = s.User.FullName,
                BranchName = s.User.Branch != null ? s.User.Branch.Name : null,
                BankName = s.User.NsUserBankAccounts.Select(b => b.BankName).FirstOrDefault(),
                BankAccountNumber = s.User.NsUserBankAccounts.Select(b => b.BankAccountNumber).FirstOrDefault(),
                BankAccountName = s.User.NsUserBankAccounts.Select(b => b.BankAccountName).FirstOrDefault(),
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

    public async Task<SalaryRuleAdjustmentPageDto> GetRuleAdjustmentsAsync(int branchId, int month, int year)
    {
        var rule = await _context.LuongSalaryRules
            .AsNoTracking()
            .Where(r => r.BranchId == branchId)
            .OrderByDescending(r => r.Id)
            .FirstOrDefaultAsync();

        var users = await _context.NsUsers
            .AsNoTracking()
            .Include(u => u.Role)
            .Where(u => u.BranchId == branchId)
            .Where(u => u.Role == null || (u.Role.RoleName != "ADMIN" && u.Role.RoleName != "MANAGER"))
            .OrderBy(u => u.FullName ?? u.Email ?? u.PhoneNumber)
            .ToListAsync();

        var employees = new List<SalaryRuleAdjustmentDto>();
        foreach (var user in users)
        {
            employees.Add(await BuildAdjustmentDtoAsync(user, month, year, rule));
        }

        return new SalaryRuleAdjustmentPageDto
        {
            Rule = rule == null ? null : ToRuleDto(rule),
            Employees = employees
        };
    }

    public async Task<SalaryRuleAdjustmentDto?> ApplyRuleAdjustmentAsync(int branchId, ApplySalaryRuleDto dto)
    {
        var user = await _context.NsUsers
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == dto.UserId && u.BranchId == branchId);
        if (user == null)
            return null;

        var roleName = user.Role?.RoleName?.ToUpperInvariant();
        if (roleName == "ADMIN" || roleName == "MANAGER")
            return null;

        var rule = await _context.LuongSalaryRules
            .Where(r => r.BranchId == branchId)
            .OrderByDescending(r => r.Id)
            .FirstOrDefaultAsync();
        if (rule == null)
            throw new InvalidOperationException("Chưa có salary rule cho cơ sở này.");

        var preview = await BuildAdjustmentDtoAsync(user, dto.Month, dto.Year, rule);
        var salary = await _context.LuongMonthlySalaries
            .FirstOrDefaultAsync(s => s.UserId == dto.UserId && s.Month == dto.Month && s.Year == dto.Year);

        if (salary != null && string.Equals(salary.Status, "PAID", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Bảng lương đã thanh toán, không thể cập nhật thưởng phạt.");

        if (salary == null)
        {
            salary = new LuongMonthlySalary
            {
                UserId = dto.UserId,
                Month = dto.Month,
                Year = dto.Year,
                TotalHours = preview.TotalHours,
                HourlyWageAtTime = user.Role?.HourlyWage ?? 0,
                Status = "PENDING",
                CreatedAt = DateTime.UtcNow
            };
            _context.LuongMonthlySalaries.Add(salary);
        }

        salary.TotalBonus = preview.CalculatedBonus;
        salary.TotalPenalty = preview.CalculatedPenalty;
        salary.HourlyWageAtTime = user.Role?.HourlyWage ?? salary.HourlyWageAtTime;
        salary.TotalSalary = (salary.TotalHours * salary.HourlyWageAtTime)
            + (salary.TotalBonus ?? 0)
            - (salary.TotalPenalty ?? 0);

        await _context.SaveChangesAsync();

        return await BuildAdjustmentDtoAsync(user, dto.Month, dto.Year, rule);
    }

    public async Task<SalaryRuleAdjustmentDto?> AddManualAdjustmentAsync(int branchId, ManualSalaryAdjustmentDto dto)
    {
        if (dto.BonusAmount < 0 || dto.PenaltyAmount < 0)
            throw new InvalidOperationException("Số tiền thưởng/phạt không được âm.");

        if (dto.BonusAmount == 0 && dto.PenaltyAmount == 0)
            throw new InvalidOperationException("Vui lòng nhập số tiền thưởng hoặc phạt.");

        var user = await _context.NsUsers
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == dto.UserId && u.BranchId == branchId);
        if (user == null)
            return null;

        var roleName = user.Role?.RoleName?.ToUpperInvariant();
        if (roleName == "ADMIN" || roleName == "MANAGER")
            return null;

        var salary = await _context.LuongMonthlySalaries
            .FirstOrDefaultAsync(s => s.UserId == dto.UserId && s.Month == dto.Month && s.Year == dto.Year);

        if (salary != null && string.Equals(salary.Status, "PAID", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Bảng lương đã thanh toán, không thể cập nhật thưởng phạt.");

        if (salary == null)
        {
            salary = new LuongMonthlySalary
            {
                UserId = dto.UserId,
                Month = dto.Month,
                Year = dto.Year,
                TotalHours = 0,
                HourlyWageAtTime = user.Role?.HourlyWage ?? 0,
                TotalBonus = 0,
                TotalPenalty = 0,
                TotalSalary = 0,
                Status = "PENDING",
                CreatedAt = DateTime.UtcNow
            };
            _context.LuongMonthlySalaries.Add(salary);
        }

        salary.TotalBonus = (salary.TotalBonus ?? 0) + dto.BonusAmount;
        salary.TotalPenalty = (salary.TotalPenalty ?? 0) + dto.PenaltyAmount;
        salary.HourlyWageAtTime = user.Role?.HourlyWage ?? salary.HourlyWageAtTime;
        salary.TotalSalary = (salary.TotalHours * salary.HourlyWageAtTime)
            + (salary.TotalBonus ?? 0)
            - (salary.TotalPenalty ?? 0);

        await _context.SaveChangesAsync();

        var rule = await _context.LuongSalaryRules
            .AsNoTracking()
            .Where(r => r.BranchId == branchId)
            .OrderByDescending(r => r.Id)
            .FirstOrDefaultAsync();
        return await BuildAdjustmentDtoAsync(user, dto.Month, dto.Year, rule);
    }

    public async Task<SalaryDto?> MarkPaidAsync(int salaryId)
    {
        var salary = await _context.LuongMonthlySalaries
            .Include(s => s.User)
            .ThenInclude(u => u.Branch)
            .Include(s => s.User)
            .ThenInclude(u => u.NsUserBankAccounts)
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
            Username = s.User?.Email ?? s.User?.PhoneNumber,
            FullName = s.User?.FullName,
            BranchName = s.User?.Branch?.Name,
            BankName = s.User?.NsUserBankAccounts.FirstOrDefault()?.BankName,
            BankAccountNumber = s.User?.NsUserBankAccounts.FirstOrDefault()?.BankAccountNumber,
            BankAccountName = s.User?.NsUserBankAccounts.FirstOrDefault()?.BankAccountName,
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

    private async Task<SalaryRuleAdjustmentDto> BuildAdjustmentDtoAsync(NsUser user, int month, int year, LuongSalaryRule? rule)
    {
        var schedules = await _context.CaFinalSchedules
            .AsNoTracking()
            .Include(s => s.Shift)
            .Include(s => s.CaAttendances)
            .Where(s => s.UserId == user.Id && s.WorkDate.Month == month && s.WorkDate.Year == year)
            .ToListAsync();

        var salary = await _context.LuongMonthlySalaries
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == user.Id && s.Month == month && s.Year == year);

        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        var workedDays = schedules
            .Where(s => s.CaAttendances.Any(a => a.CheckOutTime != null))
            .Select(s => s.WorkDate)
            .Distinct()
            .Count();
        var absentCount = schedules.Count(s => s.WorkDate <= today && !s.CaAttendances.Any(a => a.CheckOutTime != null));
        var lateCount = schedules.Count(s =>
        {
            var checkIn = s.CaAttendances
                .Where(a => a.CheckInTime != null)
                .OrderBy(a => a.CheckInTime)
                .Select(a => a.CheckInTime)
                .FirstOrDefault();
            if (checkIn == null)
                return false;

            var vietnamCheckIn = checkIn.Value.AddHours(7);
            return TimeOnly.FromDateTime(vietnamCheckIn) > s.Shift.StartTime;
        });

        var calculatedBonus = rule != null && workedDays >= (rule.BonusThresholdDays ?? 0)
            ? rule.BonusAmount ?? 0
            : 0;
        var calculatedPenalty = rule == null
            ? 0
            : (lateCount * (rule.LatePenalty ?? 0)) + (absentCount * (rule.AbsentPenalty ?? 0));

        return new SalaryRuleAdjustmentDto
        {
            UserId = user.Id,
            SalaryId = salary?.Id,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            RoleName = user.Role?.RoleName,
            Month = month,
            Year = year,
            WorkedDays = workedDays,
            LateCount = lateCount,
            AbsentCount = absentCount,
            CurrentBonus = salary?.TotalBonus ?? 0,
            CurrentPenalty = salary?.TotalPenalty ?? 0,
            CalculatedBonus = calculatedBonus,
            CalculatedPenalty = calculatedPenalty,
            TotalHours = salary?.TotalHours ?? 0,
            HourlyWageAtTime = salary?.HourlyWageAtTime ?? user.Role?.HourlyWage ?? 0,
            TotalSalary = salary?.TotalSalary ?? 0,
            Status = salary?.Status ?? "PENDING"
        };
    }

    private static SalaryRuleDto ToRuleDto(LuongSalaryRule rule)
    {
        return new SalaryRuleDto
        {
            Id = rule.Id,
            BranchId = rule.BranchId,
            BonusThresholdDays = rule.BonusThresholdDays ?? 0,
            BonusAmount = rule.BonusAmount ?? 0,
            LatePenalty = rule.LatePenalty ?? 0,
            AbsentPenalty = rule.AbsentPenalty ?? 0,
            WeekendMultiplier = rule.WeekendMultiplier ?? 1
        };
    }
}

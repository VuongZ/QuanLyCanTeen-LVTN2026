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

    public async Task<List<SalaryDto>> GetByBranchAsync(int branchId)
    {
        return await _context.LuongMonthlySalaries
            .AsNoTracking()
            .Where(s => s.User.BranchId == branchId)
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

    public async Task<List<BranchSalarySummaryDto>> GetBranchSummariesAsync()
    {
        var summaries = await _context.LuongMonthlySalaries
            .AsNoTracking()
            .GroupBy(s => new
            {
                s.User.BranchId,
                BranchName = s.User.Branch != null ? s.User.Branch.Name : null,
                s.Month,
                s.Year
            })
            .Select(g => new BranchSalarySummaryDto
            {
                BranchId = g.Key.BranchId,
                BranchName = g.Key.BranchName,
                Month = g.Key.Month,
                Year = g.Key.Year,
                SalaryCount = g.Count(),
                PendingTotal = g.Sum(s => (s.Status ?? "").ToUpper() == "PAID" ? 0 : s.TotalSalary),
                PaidTotal = g.Sum(s => (s.Status ?? "").ToUpper() == "PAID" ? s.TotalSalary : 0),
                TotalSalary = g.Sum(s => s.TotalSalary),
                PendingCount = g.Count(s => (s.Status ?? "").ToUpper() != "PAID"),
                PaidCount = g.Count(s => (s.Status ?? "").ToUpper() == "PAID"),
                EmployeeCount = g.Select(s => s.UserId).Distinct().Count()
            })
            .OrderByDescending(s => s.Year)
            .ThenByDescending(s => s.Month)
            .ThenBy(s => s.BranchName ?? "Chua gan co so")
            .ToListAsync();

        var branchIds = summaries
            .Where(s => s.BranchId != null)
            .Select(s => s.BranchId!.Value)
            .Distinct()
            .ToList();

        var managers = await _context.NsUsers
            .AsNoTracking()
            .Include(u => u.Role)
            .Include(u => u.NsUserBankAccounts)
            .Where(u => u.BranchId != null && branchIds.Contains(u.BranchId.Value))
            .Where(u => u.Role != null && u.Role.RoleName.ToUpper().Contains("MANAGER"))
            .OrderBy(u => u.FullName ?? u.Email ?? u.PhoneNumber)
            .Select(u => new
            {
                u.Id,
                u.BranchId,
                u.FullName,
                u.Email,
                u.PhoneNumber,
                BankName = u.NsUserBankAccounts.Select(b => b.BankName).FirstOrDefault(),
                BankAccountNumber = u.NsUserBankAccounts.Select(b => b.BankAccountNumber).FirstOrDefault(),
                BankAccountName = u.NsUserBankAccounts.Select(b => b.BankAccountName).FirstOrDefault()
            })
            .ToListAsync();

        var managerByBranch = managers
            .Where(m => m.BranchId != null)
            .GroupBy(m => m.BranchId!.Value)
            .ToDictionary(g => g.Key, g => g.First());

        var transfers = await _context.LuongSalaryTransfers
            .AsNoTracking()
            .Include(t => t.TransferredByUser)
            .Where(t => branchIds.Contains(t.BranchId))
            .ToListAsync();

        var transferByPeriod = transfers.ToDictionary(
            t => (t.BranchId, t.Month, t.Year),
            t => t);

        foreach (var summary in summaries)
        {
            if (summary.BranchId == null || !managerByBranch.TryGetValue(summary.BranchId.Value, out var manager))
                continue;

            summary.ManagerId = manager.Id;
            summary.ManagerName = manager.FullName;
            summary.ManagerEmail = manager.Email;
            summary.ManagerPhoneNumber = manager.PhoneNumber;
            summary.ManagerBankName = manager.BankName;
            summary.ManagerBankAccountNumber = manager.BankAccountNumber;
            summary.ManagerBankAccountName = manager.BankAccountName;

            if (transferByPeriod.TryGetValue((summary.BranchId.Value, summary.Month, summary.Year), out var transfer))
            {
                summary.TransferId = transfer.Id;
                summary.IsTransferred = true;
                summary.TransferredAmount = transfer.TotalAmount;
                summary.TransferredAt = transfer.TransferredAt;
                summary.TransferredByName = transfer.TransferredByUser.FullName
                    ?? transfer.TransferredByUser.Email
                    ?? transfer.TransferredByUser.PhoneNumber;
            }
        }

        return summaries;
    }

    public async Task<BranchSalarySummaryDto?> MarkBranchTransferredAsync(
        int branchId,
        int month,
        int year,
        int adminUserId)
    {
        if (month < 1 || month > 12 || year < 2000)
            throw new InvalidOperationException("Kỳ lương không hợp lệ.");

        var existingTransfer = await _context.LuongSalaryTransfers
            .AsNoTracking()
            .AnyAsync(t => t.BranchId == branchId && t.Month == month && t.Year == year);

        if (!existingTransfer)
        {
            var salaries = await _context.LuongMonthlySalaries
                .Where(s => s.User.BranchId == branchId && s.Month == month && s.Year == year)
                .ToListAsync();

            if (salaries.Count == 0)
                return null;

            var manager = await _context.NsUsers
                .AsNoTracking()
                .Include(u => u.Role)
                .Where(u => u.BranchId == branchId && u.IsDeleted != true)
                .Where(u => u.Role != null && u.Role.RoleName.ToUpper().Contains("MANAGER"))
                .OrderBy(u => u.Id)
                .FirstOrDefaultAsync();

            if (manager == null)
                throw new InvalidOperationException("Cơ sở chưa có quản lý để nhận quỹ lương.");

            _context.LuongSalaryTransfers.Add(new LuongSalaryTransfer
            {
                BranchId = branchId,
                ManagerId = manager.Id,
                TransferredByUserId = adminUserId,
                Month = month,
                Year = year,
                SalaryCount = salaries.Count,
                TotalAmount = salaries.Sum(s => s.TotalSalary),
                TransferredAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
        }

        return (await GetBranchSummariesAsync())
            .FirstOrDefault(s => s.BranchId == branchId && s.Month == month && s.Year == year);
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

    public async Task<SalaryRuleDto> UpsertSalaryRuleAsync(UpdateSalaryRuleDto dto)
    {
        if (dto.BonusThresholdDays < 0)
            throw new InvalidOperationException("Số ngày đạt thưởng không được âm.");

        if (dto.BonusAmount < 0 || dto.LatePenalty < 0 || dto.AbsentPenalty < 0)
            throw new InvalidOperationException("Số tiền thưởng/phạt không được âm.");

        if (dto.WeekendMultiplier <= 0)
            throw new InvalidOperationException("Hệ số cuối tuần phải lớn hơn 0.");

        var branchExists = await _context.DmBranches.AnyAsync(b => b.Id == dto.BranchId);
        if (!branchExists)
            throw new InvalidOperationException("Không tìm thấy cơ sở.");

        var rule = await _context.LuongSalaryRules
            .Where(r => r.BranchId == dto.BranchId)
            .OrderByDescending(r => r.Id)
            .FirstOrDefaultAsync();

        if (rule == null)
        {
            rule = new LuongSalaryRule
            {
                BranchId = dto.BranchId
            };
            _context.LuongSalaryRules.Add(rule);
        }

        rule.BonusThresholdDays = dto.BonusThresholdDays;
        rule.BonusAmount = dto.BonusAmount;
        rule.LatePenalty = dto.LatePenalty;
        rule.AbsentPenalty = dto.AbsentPenalty;
        rule.WeekendMultiplier = dto.WeekendMultiplier;

        await _context.SaveChangesAsync();

        return ToRuleDto(rule);
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
            var hourlyWage = SalaryWagePolicy.GetHourlyWage(
                user,
                new DateOnly(dto.Year, dto.Month, DateTime.DaysInMonth(dto.Year, dto.Month)));
            salary = new LuongMonthlySalary
            {
                UserId = dto.UserId,
                Month = dto.Month,
                Year = dto.Year,
                TotalHours = preview.TotalHours,
                HourlyWageAtTime = hourlyWage,
                Status = "PENDING",
                CreatedAt = DateTime.UtcNow
            };
            _context.LuongMonthlySalaries.Add(salary);
        }

        salary.TotalBonus = preview.CalculatedBonus;
        salary.TotalPenalty = preview.CalculatedPenalty;
        salary.HourlyWageAtTime = SalaryWagePolicy.GetHourlyWage(
            user,
            new DateOnly(dto.Year, dto.Month, DateTime.DaysInMonth(dto.Year, dto.Month)));
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
            var hourlyWage = SalaryWagePolicy.GetHourlyWage(
                user,
                new DateOnly(dto.Year, dto.Month, DateTime.DaysInMonth(dto.Year, dto.Month)));
            salary = new LuongMonthlySalary
            {
                UserId = dto.UserId,
                Month = dto.Month,
                Year = dto.Year,
                TotalHours = 0,
                HourlyWageAtTime = hourlyWage,
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
        salary.HourlyWageAtTime = SalaryWagePolicy.GetHourlyWage(
            user,
            new DateOnly(dto.Year, dto.Month, DateTime.DaysInMonth(dto.Year, dto.Month)));
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

    public async Task<SalaryDto?> MarkPaidAsync(int salaryId, int branchId)
    {
        var salary = await _context.LuongMonthlySalaries
            .Include(s => s.User)
            .ThenInclude(u => u.Branch)
            .Include(s => s.User)
            .ThenInclude(u => u.NsUserBankAccounts)
            .FirstOrDefaultAsync(s => s.Id == salaryId && s.User.BranchId == branchId);

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
            HourlyWageAtTime = salary?.HourlyWageAtTime
                ?? SalaryWagePolicy.GetHourlyWage(
                    user,
                    new DateOnly(year, month, DateTime.DaysInMonth(year, month))),
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

using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.backend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Services;

public class SalaryService
{
    private const string AdjustmentPending = "PENDING";
    private const string AdjustmentApproved = "APPROVED";
    private const string AdjustmentRejected = "REJECTED";

    private readonly AppDbContext _context;

    public SalaryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<SalaryDto>> GetByUserAsync(
        int userId,
        bool finalizedOnly = false)
    {
        var query = _context.LuongMonthlySalaries
            .AsNoTracking()
            .Where(s => s.UserId == userId);

        if (finalizedOnly)
        {
            query = query.Where(s =>
                (s.Status ?? "").ToUpper() == "FINALIZED"
                || (s.Status ?? "").ToUpper() == "PAID");
        }

        return await query
            .OrderByDescending(s => s.Year)
            .ThenByDescending(s => s.Month)
            .Select(s => new SalaryDto
            {
                Id = s.Id,
                UserId = s.UserId,
                Username = s.User.Email ?? s.User.PhoneNumber,
                FullName = s.User.FullName,
                BranchId = s.User.BranchId,
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
                FinalizedAt = s.FinalizedAt,
                FinalizedByUserId = s.FinalizedByUserId,
                FinalizedByName = s.FinalizedByUser != null
                    ? s.FinalizedByUser.FullName ?? s.FinalizedByUser.Email ?? s.FinalizedByUser.PhoneNumber
                    : null,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<List<SalaryDto>> GetAllAsync()
    {
        return await _context.LuongMonthlySalaries
            .AsNoTracking()
            .Where(s => s.User.Role == null
                || (s.User.Role.RoleName != "ADMIN" && s.User.Role.RoleName != "MANAGER"))
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
                BranchId = s.User.BranchId,
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
                FinalizedAt = s.FinalizedAt,
                FinalizedByUserId = s.FinalizedByUserId,
                FinalizedByName = s.FinalizedByUser != null
                    ? s.FinalizedByUser.FullName ?? s.FinalizedByUser.Email ?? s.FinalizedByUser.PhoneNumber
                    : null,
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
                BranchId = s.User.BranchId,
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
                FinalizedAt = s.FinalizedAt,
                FinalizedByUserId = s.FinalizedByUserId,
                FinalizedByName = s.FinalizedByUser != null
                    ? s.FinalizedByUser.FullName ?? s.FinalizedByUser.Email ?? s.FinalizedByUser.PhoneNumber
                    : null,
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
        var synchronizedSalaries = new List<(SalaryRuleAdjustmentDto Adjustment, LuongMonthlySalary Salary)>();
        foreach (var user in users)
        {
            var adjustment = await BuildAdjustmentDtoAsync(user, month, year, rule);
            employees.Add(adjustment);

            var salary = rule == null
                ? null
                : await SynchronizeRuleAdjustmentAsync(user, adjustment);
            if (salary != null)
                synchronizedSalaries.Add((adjustment, salary));
        }

        if (synchronizedSalaries.Count > 0)
        {
            await _context.SaveChangesAsync();
            foreach (var (adjustment, salary) in synchronizedSalaries)
            {
                adjustment.SalaryId = salary.Id;
                adjustment.CurrentBonus = salary.TotalBonus ?? 0;
                adjustment.CurrentPenalty = salary.TotalPenalty ?? 0;
                adjustment.TotalHours = salary.TotalHours;
                adjustment.HourlyWageAtTime = salary.HourlyWageAtTime;
                adjustment.TotalSalary = salary.TotalSalary;
                adjustment.Status = salary.Status;
            }
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
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == dto.UserId && s.Month == dto.Month && s.Year == dto.Year);

        if (salary != null && IsSalaryLocked(salary.Status))
            throw new InvalidOperationException("Bảng lương đã chốt hoặc thanh toán, không thể cập nhật thưởng phạt.");

        await SynchronizeRuleAdjustmentAsync(user, preview, createWhenNoAdjustment: true);

        await _context.SaveChangesAsync();

        return await BuildAdjustmentDtoAsync(user, dto.Month, dto.Year, rule);
    }

    public async Task<SalaryAdjustmentHistoryDto?> AddManualAdjustmentAsync(
        int branchId,
        int createdByUserId,
        ManualSalaryAdjustmentDto dto)
    {
        if (dto.BonusAmount < 0 || dto.PenaltyAmount < 0)
            throw new InvalidOperationException("Số tiền thưởng/phạt không được âm.");

        if (dto.BonusAmount == 0 && dto.PenaltyAmount == 0)
            throw new InvalidOperationException("Vui lòng nhập số tiền thưởng hoặc phạt.");

        var reason = dto.Reason?.Trim();
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Vui lòng nhập lý do thưởng/phạt.");

        if (reason.Length > 500)
            throw new InvalidOperationException("Lý do thưởng/phạt không được vượt quá 500 ký tự.");

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

        if (salary != null && IsSalaryLocked(salary.Status))
            throw new InvalidOperationException("Bảng lương đã chốt hoặc thanh toán, không thể cập nhật thưởng phạt.");

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

        var request = new LuongSalaryAdjustmentHistory
        {
            Salary = salary,
            UserId = user.Id,
            CreatedByUserId = createdByUserId,
            Month = dto.Month,
            Year = dto.Year,
            BonusAmount = dto.BonusAmount,
            PenaltyAmount = dto.PenaltyAmount,
            Reason = reason,
            Status = AdjustmentPending,
            CreatedAt = DateTime.Now
        };
        _context.LuongSalaryAdjustmentHistories.Add(request);

        await _context.SaveChangesAsync();

        return await GetAdjustmentByIdAsync(request.Id);
    }

    public async Task<List<SalaryAdjustmentHistoryDto>> GetAdjustmentHistoryAsync(
        int userId,
        int? month = null,
        int? year = null)
    {
        var query = _context.LuongSalaryAdjustmentHistories
            .AsNoTracking()
            .Where(h => h.UserId == userId);

        if (month.HasValue)
            query = query.Where(h => h.Month == month.Value);
        if (year.HasValue)
            query = query.Where(h => h.Year == year.Value);

        return await query
            .OrderByDescending(h => h.CreatedAt)
            .ThenByDescending(h => h.Id)
            .Select(h => new SalaryAdjustmentHistoryDto
            {
                Id = h.Id,
                SalaryId = h.SalaryId,
                UserId = h.UserId,
                EmployeeName = h.User.FullName ?? h.User.Email ?? h.User.PhoneNumber,
                Month = h.Month,
                Year = h.Year,
                BonusAmount = h.BonusAmount,
                PenaltyAmount = h.PenaltyAmount,
                Reason = h.Reason,
                Status = h.Status,
                CreatedByUserId = h.CreatedByUserId,
                CreatedByName = h.CreatedByUser.FullName ?? h.CreatedByUser.Email ?? h.CreatedByUser.PhoneNumber,
                BranchName = h.User.Branch != null ? h.User.Branch.Name : null,
                ReviewedByUserId = h.ReviewedByUserId,
                ReviewedByName = h.ReviewedByUser != null
                    ? h.ReviewedByUser.FullName ?? h.ReviewedByUser.Email ?? h.ReviewedByUser.PhoneNumber
                    : null,
                ReviewedAt = h.ReviewedAt,
                ReviewNote = h.ReviewNote,
                CreatedAt = h.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<List<SalaryDto>> FinalizeBranchPeriodAsync(
        int branchId,
        int month,
        int year,
        int managerUserId)
    {
        if (month < 1 || month > 12 || year < 2000 || year > 2100)
            throw new InvalidOperationException("Kỳ lương không hợp lệ.");

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var salaries = await _context.LuongMonthlySalaries
            .Include(s => s.User)
                .ThenInclude(u => u.Branch)
            .Include(s => s.User)
                .ThenInclude(u => u.Role)
            .Include(s => s.User)
                .ThenInclude(u => u.NsUserBankAccounts)
            .Include(s => s.FinalizedByUser)
            .Where(s =>
                s.User.BranchId == branchId
                && s.Month == month
                && s.Year == year
                && (s.User.Role == null
                    || (s.User.Role.RoleName != "ADMIN"
                        && s.User.Role.RoleName != "MANAGER")))
            .OrderBy(s => s.User.FullName ?? s.User.Email ?? s.User.PhoneNumber)
            .ToListAsync();

        if (salaries.Count == 0)
            throw new InvalidOperationException("Không có bảng lương nhân viên trong kỳ đã chọn.");

        var salaryIds = salaries.Select(s => s.Id).ToList();
        var pendingEmployee = await _context.LuongSalaryAdjustmentHistories
            .AsNoTracking()
            .Where(h => salaryIds.Contains(h.SalaryId) && h.Status == AdjustmentPending)
            .Select(h => h.User.FullName ?? h.User.Email ?? h.User.PhoneNumber)
            .FirstOrDefaultAsync();
        if (pendingEmployee != null)
        {
            throw new InvalidOperationException(
                $"Nhân viên {pendingEmployee} còn yêu cầu thưởng/phạt đang chờ Admin duyệt.");
        }

        var rule = await _context.LuongSalaryRules
            .AsNoTracking()
            .Where(r => r.BranchId == branchId)
            .OrderByDescending(r => r.Id)
            .FirstOrDefaultAsync();

        foreach (var salary in salaries)
        {
            var status = (salary.Status ?? "PENDING").ToUpperInvariant();
            if (status == "PAID")
                continue;
            if (status != "PENDING" && status != "FINALIZED")
                throw new InvalidOperationException($"Bảng lương của {salary.User.FullName} có trạng thái không hợp lệ.");
            if (status == "FINALIZED")
                continue;

            if (rule != null)
            {
                var adjustment = await BuildAdjustmentDtoAsync(
                    salary.User,
                    month,
                    year,
                    rule);
                await SetSalaryAdjustmentTotalsAsync(salary, adjustment);
            }

            salary.Status = "FINALIZED";
            salary.FinalizedAt = DateTime.Now;
            salary.FinalizedByUserId = managerUserId;
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return salaries.Select(ToDto).ToList();
    }

    public async Task<List<SalaryAdjustmentHistoryDto>> GetPendingAdjustmentRequestsAsync()
    {
        return await _context.LuongSalaryAdjustmentHistories
            .AsNoTracking()
            .Where(h => h.Status == AdjustmentPending)
            .OrderBy(h => h.CreatedAt)
            .ThenBy(h => h.Id)
            .Select(h => new SalaryAdjustmentHistoryDto
            {
                Id = h.Id,
                SalaryId = h.SalaryId,
                UserId = h.UserId,
                EmployeeName = h.User.FullName ?? h.User.Email ?? h.User.PhoneNumber,
                Month = h.Month,
                Year = h.Year,
                BonusAmount = h.BonusAmount,
                PenaltyAmount = h.PenaltyAmount,
                Reason = h.Reason,
                Status = h.Status,
                CreatedByUserId = h.CreatedByUserId,
                CreatedByName = h.CreatedByUser.FullName ?? h.CreatedByUser.Email ?? h.CreatedByUser.PhoneNumber,
                BranchName = h.User.Branch != null ? h.User.Branch.Name : null,
                CreatedAt = h.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<SalaryAdjustmentHistoryDto?> ReviewAdjustmentAsync(
        int adjustmentId,
        int adminUserId,
        ReviewSalaryAdjustmentDto dto)
    {
        var reviewNote = dto.ReviewNote?.Trim();
        if (reviewNote?.Length > 500)
            throw new InvalidOperationException("Ghi chú duyệt không được vượt quá 500 ký tự.");

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var adjustment = await _context.LuongSalaryAdjustmentHistories
            .Include(h => h.Salary)
            .Include(h => h.User)
                .ThenInclude(u => u.Role)
            .FirstOrDefaultAsync(h => h.Id == adjustmentId);

        if (adjustment == null)
            return null;

        if (!string.Equals(adjustment.Status, AdjustmentPending, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Yêu cầu thưởng/phạt này đã được xử lý.");

        if (IsSalaryLocked(adjustment.Salary.Status))
            throw new InvalidOperationException("Bảng lương đã chốt hoặc thanh toán, không thể duyệt yêu cầu.");

        adjustment.Status = dto.IsApproved
            ? AdjustmentApproved
            : AdjustmentRejected;
        adjustment.ReviewedByUserId = adminUserId;
        adjustment.ReviewedAt = DateTime.Now;
        adjustment.ReviewNote = reviewNote;

        await _context.SaveChangesAsync();

        if (dto.IsApproved)
        {
            var rule = await _context.LuongSalaryRules
                .AsNoTracking()
                .Where(r => r.BranchId == adjustment.User.BranchId)
                .OrderByDescending(r => r.Id)
                .FirstOrDefaultAsync();

            var calculated = await BuildAdjustmentDtoAsync(
                adjustment.User,
                adjustment.Month,
                adjustment.Year,
                rule);

            await SetSalaryAdjustmentTotalsAsync(adjustment.Salary, calculated);
            await _context.SaveChangesAsync();
        }

        await transaction.CommitAsync();
        return await GetAdjustmentByIdAsync(adjustment.Id);
    }

    public async Task<SalaryDto?> FinalizeAsync(int salaryId, int branchId, int managerUserId)
    {
        var salary = await _context.LuongMonthlySalaries
            .Include(s => s.User)
            .ThenInclude(u => u.Branch)
            .Include(s => s.User)
            .ThenInclude(u => u.NsUserBankAccounts)
            .Include(s => s.FinalizedByUser)
            .FirstOrDefaultAsync(s => s.Id == salaryId && s.User.BranchId == branchId);

        if (salary == null)
            return null;

        var status = (salary.Status ?? "PENDING").ToUpperInvariant();
        if (status == "PAID")
            throw new InvalidOperationException("Bảng lương đã thanh toán.");

        if (status != "PENDING" && status != "FINALIZED")
            throw new InvalidOperationException("Trạng thái bảng lương không cho phép chốt.");

        if (status != "FINALIZED")
        {
            var hasPendingAdjustment = await _context.LuongSalaryAdjustmentHistories
                .AnyAsync(h => h.SalaryId == salary.Id && h.Status == AdjustmentPending);
            if (hasPendingAdjustment)
                throw new InvalidOperationException("Còn yêu cầu thưởng/phạt đang chờ Admin duyệt.");

            var rule = await _context.LuongSalaryRules
                .AsNoTracking()
                .Where(r => r.BranchId == salary.User.BranchId)
                .OrderByDescending(r => r.Id)
                .FirstOrDefaultAsync();
            if (rule != null)
            {
                var adjustment = await BuildAdjustmentDtoAsync(salary.User, salary.Month, salary.Year, rule);
                await SetSalaryAdjustmentTotalsAsync(salary, adjustment);
            }

            salary.Status = "FINALIZED";
            salary.FinalizedAt = DateTime.Now;
            salary.FinalizedByUserId = managerUserId;
            await _context.SaveChangesAsync();
            var finalizedByReference = _context.Entry(salary).Reference(s => s.FinalizedByUser);
            finalizedByReference.IsLoaded = false;
            await finalizedByReference.LoadAsync();
        }

        return ToDto(salary);
    }

    public async Task<SalaryDto?> MarkPaidAsync(int salaryId, int branchId)
    {
        var salary = await _context.LuongMonthlySalaries
            .Include(s => s.User)
            .ThenInclude(u => u.Branch)
            .Include(s => s.User)
            .ThenInclude(u => u.NsUserBankAccounts)
            .Include(s => s.FinalizedByUser)
            .FirstOrDefaultAsync(s => s.Id == salaryId && s.User.BranchId == branchId);

        if (salary == null)
            return null;

        if (!string.Equals(salary.Status, "FINALIZED", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Manager phải chốt bảng lương trước khi xác nhận đã trả.");

        var hasPendingAdjustment = await _context.LuongSalaryAdjustmentHistories
            .AnyAsync(h => h.SalaryId == salary.Id && h.Status == AdjustmentPending);
        if (hasPendingAdjustment)
            throw new InvalidOperationException("Còn yêu cầu thưởng/phạt đang chờ Admin duyệt.");

        var hasPendingComplaint = await _context.LuongSalaryComplaints
            .AnyAsync(c => c.SalaryId == salary.Id && c.Status == "PENDING");
        if (hasPendingComplaint)
            throw new InvalidOperationException("Nhân viên đang có khiếu nại lương chưa được xử lý.");

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
            BranchId = s.User?.BranchId,
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
            FinalizedAt = s.FinalizedAt,
            FinalizedByUserId = s.FinalizedByUserId,
            FinalizedByName = s.FinalizedByUser?.FullName
                ?? s.FinalizedByUser?.Email
                ?? s.FinalizedByUser?.PhoneNumber,
            CreatedAt = s.CreatedAt
        };
    }

    private static bool IsSalaryLocked(string? status)
    {
        return string.Equals(status, "FINALIZED", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "PAID", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<LuongMonthlySalary?> SynchronizeRuleAdjustmentAsync(
        NsUser user,
        SalaryRuleAdjustmentDto adjustment,
        bool createWhenNoAdjustment = false)
    {
        var salary = await _context.LuongMonthlySalaries
            .FirstOrDefaultAsync(s =>
                s.UserId == user.Id
                && s.Month == adjustment.Month
                && s.Year == adjustment.Year);

        if (salary != null && IsSalaryLocked(salary.Status))
            return null;

        if (salary == null)
        {
            if (!createWhenNoAdjustment
                && adjustment.CalculatedBonus == 0
                && adjustment.CalculatedPenalty == 0)
            {
                return null;
            }

            salary = new LuongMonthlySalary
            {
                UserId = user.Id,
                Month = adjustment.Month,
                Year = adjustment.Year,
                TotalHours = adjustment.TotalHours,
                HourlyWageAtTime = adjustment.HourlyWageAtTime,
                Status = "PENDING",
                CreatedAt = DateTime.UtcNow
            };
            _context.LuongMonthlySalaries.Add(salary);
        }

        await SetSalaryAdjustmentTotalsAsync(salary, adjustment);
        return salary;
    }

    private async Task SetSalaryAdjustmentTotalsAsync(
        LuongMonthlySalary salary,
        SalaryRuleAdjustmentDto adjustment)
    {
        var manualTotals = await GetManualAdjustmentTotalsAsync(
            salary.UserId,
            salary.Month,
            salary.Year);

        salary.TotalBonus = adjustment.CalculatedBonus + manualTotals.Bonus;
        salary.TotalPenalty = adjustment.CalculatedPenalty + manualTotals.Penalty;
        salary.HourlyWageAtTime = adjustment.HourlyWageAtTime;
        salary.TotalSalary = (salary.TotalHours * salary.HourlyWageAtTime)
            + (salary.TotalBonus ?? 0)
            - (salary.TotalPenalty ?? 0);
    }

    private async Task<(decimal Bonus, decimal Penalty)> GetManualAdjustmentTotalsAsync(
        int userId,
        int month,
        int year)
    {
        var totals = await _context.LuongSalaryAdjustmentHistories
            .AsNoTracking()
            .Where(h =>
                h.UserId == userId
                && h.Month == month
                && h.Year == year
                && h.Status == AdjustmentApproved)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Bonus = g.Sum(h => h.BonusAmount),
                Penalty = g.Sum(h => h.PenaltyAmount)
            })
            .FirstOrDefaultAsync();

        return totals == null
            ? (0, 0)
            : (totals.Bonus, totals.Penalty);
    }

    private async Task<SalaryAdjustmentHistoryDto?> GetAdjustmentByIdAsync(int adjustmentId)
    {
        return await _context.LuongSalaryAdjustmentHistories
            .AsNoTracking()
            .Where(h => h.Id == adjustmentId)
            .Select(h => new SalaryAdjustmentHistoryDto
            {
                Id = h.Id,
                SalaryId = h.SalaryId,
                UserId = h.UserId,
                EmployeeName = h.User.FullName ?? h.User.Email ?? h.User.PhoneNumber,
                Month = h.Month,
                Year = h.Year,
                BonusAmount = h.BonusAmount,
                PenaltyAmount = h.PenaltyAmount,
                Reason = h.Reason,
                Status = h.Status,
                CreatedByUserId = h.CreatedByUserId,
                CreatedByName = h.CreatedByUser.FullName ?? h.CreatedByUser.Email ?? h.CreatedByUser.PhoneNumber,
                BranchName = h.User.Branch != null ? h.User.Branch.Name : null,
                ReviewedByUserId = h.ReviewedByUserId,
                ReviewedByName = h.ReviewedByUser != null
                    ? h.ReviewedByUser.FullName ?? h.ReviewedByUser.Email ?? h.ReviewedByUser.PhoneNumber
                    : null,
                ReviewedAt = h.ReviewedAt,
                ReviewNote = h.ReviewNote,
                CreatedAt = h.CreatedAt
            })
            .FirstOrDefaultAsync();
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
            .Where(s => s.CaAttendances.Any(a =>
                a.CheckOutTime != null && a.Status != CheckoutRequestService.AutoCheckoutPending))
            .Select(s => s.WorkDate)
            .Distinct()
            .Count();
        bool IsAbsent(CaFinalSchedule schedule) =>
            schedule.WorkDate <= today && !schedule.CaAttendances.Any(a =>
                a.CheckOutTime != null && a.Status != CheckoutRequestService.AutoCheckoutPending);

        DateTime? FirstCheckIn(CaFinalSchedule schedule) => schedule.CaAttendances
            .Where(a => a.CheckInTime != null)
            .OrderBy(a => a.CheckInTime)
            .Select(a => a.CheckInTime)
            .FirstOrDefault();

        bool IsLate(CaFinalSchedule schedule)
        {
            var checkIn = FirstCheckIn(schedule);
            if (checkIn == null)
                return false;

            var vietnamCheckIn = checkIn.Value.AddHours(7);
            return TimeOnly.FromDateTime(vietnamCheckIn) > schedule.Shift.StartTime;
        }

        AttendanceIssueDetailDto ToIssueDetail(CaFinalSchedule schedule)
        {
            var checkIn = FirstCheckIn(schedule);
            return new AttendanceIssueDetailDto
            {
                WorkDate = schedule.WorkDate,
                ShiftName = schedule.Shift.ShiftName,
                ScheduledTime = $"{schedule.Shift.StartTime:HH\\:mm} - {schedule.Shift.EndTime:HH\\:mm}",
                ActualCheckInTime = checkIn?.AddHours(7).ToString("HH:mm")
            };
        }

        var absentDetails = schedules
            .Where(IsAbsent)
            .OrderBy(s => s.WorkDate)
            .ThenBy(s => s.Shift.StartTime)
            .Select(ToIssueDetail)
            .ToList();
        var lateDetails = schedules
            .Where(IsLate)
            .OrderBy(s => s.WorkDate)
            .ThenBy(s => s.Shift.StartTime)
            .Select(ToIssueDetail)
            .ToList();
        var absentCount = absentDetails.Count;
        var lateCount = lateDetails.Count;

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
            LateDetails = lateDetails,
            AbsentDetails = absentDetails,
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

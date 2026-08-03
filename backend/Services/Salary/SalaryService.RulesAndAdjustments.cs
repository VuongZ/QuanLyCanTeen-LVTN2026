using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.backend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Services;

public partial class SalaryService
{
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
    var status =
        (salary.Status ?? "PENDING")
            .ToUpperInvariant();

    if (status == "PAID")
    {
        continue;
    }

    if (
        status != "PENDING" &&
        status != "FINALIZED"
    )
    {
        throw new InvalidOperationException(
            $"Bảng lương của {salary.User.FullName} " +
            "có trạng thái không hợp lệ.");
    }

    /*
      Liên kết và lấy khoản khấu trừ BHXH.
    */
    await ApplySocialInsuranceDeductionAsync(
        salary
    );

    /*
      Nếu bảng lương đã chốt từ trước,
      chỉ cập nhật thông tin BHXH rồi bỏ qua
      việc chốt trạng thái lần nữa.
    */
    if (status == "FINALIZED")
    {
        continue;
    }

    if (rule != null)
    {
        var adjustment =
            await BuildAdjustmentDtoAsync(
                salary.User,
                month,
                year,
                rule);

        await SetSalaryAdjustmentTotalsAsync(
            salary,
            adjustment);
    }

    salary.Status = "FINALIZED";
    salary.FinalizedAt = DateTime.Now;
    salary.FinalizedByUserId =
        managerUserId;
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


/// Manager chốt một bảng lương.
///
/// Khi chốt:
/// - Kiểm tra yêu cầu thưởng/phạt.
/// - Tính lại thưởng, phạt và tổng lương.
/// - Liên kết khoản đóng BHXH đối với FULL_TIME.
/// - Lưu phần BHXH nhân viên phải đóng.
/// - Chuyển trạng thái sang FINALIZED.
}


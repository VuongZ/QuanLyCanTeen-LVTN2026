using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.backend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Services;

public partial class SalaryService
{
private async Task SynchronizeCurrentPendingSalarySnapshotsAsync(
    int? userId = null,
    int? branchId = null)
{
    var today =
        DateOnly.FromDateTime(
            DateTime.UtcNow.AddHours(7));

    var salaryQuery =
        _context.LuongMonthlySalaries
            .Include(salary =>
                salary.User)
                .ThenInclude(user =>
                    user.Role)
            .Where(salary =>
                salary.Month == today.Month &&
                salary.Year == today.Year &&
                (salary.Status == null ||
                 salary.Status.ToUpper() == "PENDING"));

    if (userId.HasValue)
    {
        salaryQuery = salaryQuery.Where(salary =>
            salary.UserId == userId.Value);
    }

    if (branchId.HasValue)
    {
        salaryQuery = salaryQuery.Where(salary =>
            salary.User.BranchId == branchId.Value);
    }

    var salaries =
        await salaryQuery.ToListAsync();

    if (salaries.Count == 0)
    {
        return;
    }

    var fullTimeUserIds =
        salaries
            .Where(salary =>
                SalaryWagePolicy.IsFullTimeEquivalent(
                    salary.User.EmploymentType))
            .Select(salary =>
                salary.UserId)
            .Distinct()
            .ToList();

    var contributions =
        await _context.BhxhMonthlyContributions
            .AsNoTracking()
            .Where(item =>
                fullTimeUserIds.Contains(item.UserId) &&
                item.Month == today.Month &&
                item.Year == today.Year &&
                (item.Status.ToUpper() == "CONFIRMED" ||
                 item.Status.ToUpper() == "PAID"))
            .ToListAsync();

    var contributionByUserId =
        contributions
            .GroupBy(item =>
                item.UserId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(item =>
                        item.Status.ToUpper() == "PAID")
                    .ThenByDescending(item =>
                        item.Id)
                    .First());

    foreach (var salary in salaries)
    {
        salary.HourlyWageAtTime =
            SalaryWagePolicy.GetHourlyWage(
                salary.User,
                today);

        salary.TotalSalary =
            salary.TotalHours * salary.HourlyWageAtTime +
            (salary.TotalBonus ?? 0) -
            (salary.TotalPenalty ?? 0);

        if (SalaryWagePolicy.IsFullTimeEquivalent(
                salary.User.EmploymentType) &&
            contributionByUserId.TryGetValue(
                salary.UserId,
                out var contribution))
        {
            salary.BhxhContributionId = contribution.Id;
            salary.SocialInsuranceDeduction =
                contribution.EmployeeAmount;
        }
        else
        {
            salary.BhxhContributionId = null;
            salary.SocialInsuranceDeduction = 0;
        }
    }

    await _context.SaveChangesAsync();
}

private static SalaryDto ToDto(
    LuongMonthlySalary s)
{
    return new SalaryDto
    {
        Id = s.Id,

        UserId = s.UserId,

        Username =
            s.User?.Email ??
            s.User?.PhoneNumber,

        FullName =
            s.User?.FullName,

        BranchId =
            s.User?.BranchId,

        BranchName =
            s.User?.Branch?.Name,

        EmploymentType =
            s.User?.EmploymentType ?? SalaryWagePolicy.PartTime,

        BankName =
            s.User
                ?.NsUserBankAccounts
                .FirstOrDefault()
                ?.BankName,

        BankAccountNumber =
            s.User
                ?.NsUserBankAccounts
                .FirstOrDefault()
                ?.BankAccountNumber,

        BankAccountName =
            s.User
                ?.NsUserBankAccounts
                .FirstOrDefault()
                ?.BankAccountName,

        Month = s.Month,

        Year = s.Year,

        TotalHours =
            s.TotalHours,

        HourlyWageAtTime =
            s.HourlyWageAtTime,

        // Tổng lương trước khi trừ BHXH.
        TotalSalary =
            s.TotalSalary,

        TotalBonus =
            s.TotalBonus ?? 0,

        TotalPenalty =
            s.TotalPenalty ?? 0,

        // ID khoản đóng BHXH liên kết với bảng lương.
        BhxhContributionId =
            s.BhxhContributionId,

        // Phần BHXH do nhân viên đóng.
        SocialInsuranceDeduction =
            s.SocialInsuranceDeduction,

        Status =
            s.Status,

        PaidAt =
            s.PaidAt,

        FinalizedAt =
            s.FinalizedAt,

        FinalizedByUserId =
            s.FinalizedByUserId,

        FinalizedByName =
            s.FinalizedByUser?.FullName
            ?? s.FinalizedByUser?.Email
            ?? s.FinalizedByUser?.PhoneNumber,

        CreatedAt =
            s.CreatedAt
    };
}

/// <summary>
/// Liên kết khoản đóng BHXH với bảng lương.
///
/// Quy tắc:
/// - PART_TIME không bị khấu trừ BHXH.
/// - FULL_TIME phải có khoản đóng BHXH cùng tháng.
/// - Chỉ dùng khoản có trạng thái CONFIRMED hoặc PAID.
/// - Chỉ trừ phần EmployeeAmount.
/// - EmployerAmount là phần doanh nghiệp đóng,
///   không trừ vào lương nhân viên.
/// </summary>
private async Task ApplySocialInsuranceDeductionAsync(
    LuongMonthlySalary salary)
{
    if (salary.User == null)
    {
        throw new InvalidOperationException(
            "Không xác định được nhân viên của bảng lương.");
    }

    /*
      Nhân viên PART_TIME không tham gia
      phân hệ BHXH trong phạm vi đồ án.
    */
    if (!SalaryWagePolicy.IsFullTimeEquivalent(
            salary.User.EmploymentType))
    {
        salary.BhxhContributionId = null;
        salary.SocialInsuranceDeduction = 0;

        return;
    }

    /*
      FULL_TIME phải có khoản đóng BHXH
      cùng tháng và cùng năm.

      Chỉ lấy khoản đã được Admin xác nhận
      hoặc đã được đánh dấu là đã nộp.
    */
    var contribution =
        await _context.BhxhMonthlyContributions
            .AsNoTracking()
            .Where(c =>
                c.UserId == salary.UserId
                && c.Month == salary.Month
                && c.Year == salary.Year)
            .Where(c =>
                c.Status.ToUpper() == "CONFIRMED"
                || c.Status.ToUpper() == "PAID")
            .OrderByDescending(c =>
                c.Status.ToUpper() == "PAID")
            .ThenByDescending(c => c.Id)
            .FirstOrDefaultAsync();

    if (contribution == null)
    {
        var employeeName =
            salary.User.FullName
            ?? salary.User.Email
            ?? salary.User.PhoneNumber
            ?? $"ID {salary.UserId}";

        throw new InvalidOperationException(
            $"Nhân viên {employeeName} thuộc diện FULL_TIME/Thai sản nhưng " +
            $"chưa có khoản đóng BHXH đã xác nhận cho " +
            $"tháng {salary.Month}/{salary.Year}.");
    }

    /*
      Kiểm tra khoản BHXH có đang được liên kết
      với một bảng lương khác hay không.

      Việc kiểm tra trước giúp trả thông báo rõ ràng,
      thay vì chờ database báo lỗi UNIQUE.
    */
    var linkedToAnotherSalary =
        await _context.LuongMonthlySalaries
            .AsNoTracking()
            .AnyAsync(s =>
                s.BhxhContributionId == contribution.Id
                && s.Id != salary.Id);

    if (linkedToAnotherSalary)
    {
        throw new InvalidOperationException(
            "Khoản đóng BHXH này đã được liên kết " +
            "với một bảng lương khác.");
    }

    /*
      Lưu liên kết tới khoản đóng BHXH.
    */
    salary.BhxhContributionId =
        contribution.Id;

    /*
      Chỉ khấu trừ phần nhân viên đóng.
      Không trừ EmployerAmount.
    */
    salary.SocialInsuranceDeduction =
        contribution.EmployeeAmount;
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

            var allowedCheckInTime =
                SalaryWagePolicy.NormalizeEmploymentType(
                    user.EmploymentType) == SalaryWagePolicy.Maternity
                    ? schedule.Shift.StartTime.AddMinutes(
                        AttendanceWorkHourPolicy.MaternityGraceMinutes)
                    : schedule.Shift.StartTime;

            return TimeOnly.FromDateTime(checkIn.Value) > allowedCheckInTime;
        }

        AttendanceIssueDetailDto ToIssueDetail(CaFinalSchedule schedule)
        {
            var checkIn = FirstCheckIn(schedule);
            return new AttendanceIssueDetailDto
            {
                WorkDate = schedule.WorkDate,
                ShiftName = schedule.Shift.ShiftName,
                ScheduledTime = $"{schedule.Shift.StartTime:HH\\:mm} - {schedule.Shift.EndTime:HH\\:mm}",
                ActualCheckInTime = checkIn?.ToString("HH:mm")
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

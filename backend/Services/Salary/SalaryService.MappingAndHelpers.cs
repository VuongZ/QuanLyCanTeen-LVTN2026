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

    var eligibleUserIds =
        salaries
            .Where(salary =>
                SalaryWagePolicy.IsSocialInsuranceEligible(
                    salary.User.EmploymentType))
            .Select(salary =>
                salary.UserId)
            .Distinct()
            .ToList();

    var contributions =
        await _context.BhxhMonthlyContributions
            .AsNoTracking()
            .Where(item =>
                eligibleUserIds.Contains(item.UserId) &&
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

        if (SalaryWagePolicy.IsSocialInsuranceEligible(
                salary.User.EmploymentType) &&
            contributionByUserId.TryGetValue(
                salary.UserId,
                out var contribution))
        {
            salary.BhxhContributionId =
                contribution.Id;

            // Chỉ hiển thị đúng số tiền đã thực tế
            // khấu trừ khỏi lương nhân viên.
            salary.SocialInsuranceDeduction =
                contribution.EmployeeDeductedAmount;
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

        // ID khoản đóng BHXH phát sinh trong tháng lương.
BhxhContributionId =
    s.BhxhContributionId,

// BHXH phát sinh trong chính tháng lương.
CurrentBhxhDeduction =
    s.CurrentBhxhDeduction,

// Khoản doanh nghiệp ứng trước từ các tháng cũ
// được thu hồi trong bảng lương này.
PreviousBhxhRecovery =
    s.PreviousBhxhRecovery,

// Tổng khấu trừ BHXH trên bảng lương.
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
/// Tìm khoản đóng BHXH của tháng hoặc tự động tạo mới
/// từ hồ sơ ACTIVE và cấu hình tỷ lệ có hiệu lực.
/// </summary>
private async Task<BhxhMonthlyContribution>
    GetOrCreateSocialInsuranceContributionAsync(
        LuongMonthlySalary salary)
{
    var existingContribution =
        await _context.BhxhMonthlyContributions
            .FirstOrDefaultAsync(contribution =>
                contribution.UserId == salary.UserId &&
                contribution.Month == salary.Month &&
                contribution.Year == salary.Year);

    if (existingContribution != null)
    {
        return existingContribution;
    }

    var firstDayOfMonth =
        new DateOnly(
            salary.Year,
            salary.Month,
            1);

    var lastDayOfMonth =
        firstDayOfMonth
            .AddMonths(1)
            .AddDays(-1);

    var profile =
        await _context.BhxhEmployeeProfiles
            .FirstOrDefaultAsync(item =>
                item.UserId == salary.UserId &&
                item.Status.ToUpper() == "ACTIVE" &&
                item.StaffConfirmationStatus.ToUpper() ==
                    "CONFIRMED" &&
                item.StartDate <= lastDayOfMonth &&
                (item.EndDate == null ||
                 item.EndDate >= firstDayOfMonth));

    if (profile == null)
    {
        var employeeName =
            salary.User?.FullName
            ?? salary.User?.Email
            ?? salary.User?.PhoneNumber
            ?? $"ID {salary.UserId}";

        throw new InvalidOperationException(
            $"Nhân viên {employeeName} chưa có hồ sơ BHXH " +
            $"ACTIVE và đã được xác nhận cho " +
            $"tháng {salary.Month}/{salary.Year}.");
    }

    if (profile.InsuranceSalaryBasis <= 0)
    {
        throw new InvalidOperationException(
            "Mức lương làm căn cứ đóng BHXH không hợp lệ.");
    }

    var rateConfig =
        await _context.BhxhRateConfigs
            .AsNoTracking()
            .Where(rate =>
                rate.EffectiveFrom <= firstDayOfMonth &&
                (rate.EffectiveTo == null ||
                 rate.EffectiveTo >= firstDayOfMonth))
            .OrderByDescending(rate =>
                rate.EffectiveFrom)
            .FirstOrDefaultAsync();

    if (rateConfig == null)
    {
        throw new InvalidOperationException(
            $"Không tìm thấy cấu hình tỷ lệ BHXH " +
            $"có hiệu lực trong tháng " +
            $"{salary.Month}/{salary.Year}.");
    }

    var employeeAmount =
        Math.Round(
            profile.InsuranceSalaryBasis *
            rateConfig.EmployeeRate /
            100m,
            2,
            MidpointRounding.AwayFromZero);

    var employerAmount =
        Math.Round(
            profile.InsuranceSalaryBasis *
            rateConfig.EmployerRate /
            100m,
            2,
            MidpointRounding.AwayFromZero);

    var now =
        DateTime.UtcNow.AddHours(7);

    var contribution =
        new BhxhMonthlyContribution
        {
            UserId =
                salary.UserId,

            ProfileId =
                profile.Id,

            RateConfigId =
                rateConfig.Id,

            Month =
                (sbyte)salary.Month,

            Year =
                (short)salary.Year,

            InsuranceSalaryBasis =
                profile.InsuranceSalaryBasis,

            EmployeeRate =
                rateConfig.EmployeeRate,

            EmployerRate =
                rateConfig.EmployerRate,

            EmployeeAmount =
                employeeAmount,

            EmployeeDeductedAmount =
                0,

            EmployeeOutstandingAmount =
                employeeAmount,

            DeductionStatus =
                "NONE",

            EmployerAmount =
                employerAmount,

            TotalAmount =
                employeeAmount + employerAmount,

            Status =
                "DRAFT",

            CreatedAt = now,
            UpdatedAt = now
        };

    await _context.BhxhMonthlyContributions
        .AddAsync(contribution);

    return contribution;
}

/// <summary>
/// Thêm ghi chú nhưng không lặp lại cùng một nội dung.
/// </summary>
private static void AppendContributionNote(
    BhxhMonthlyContribution contribution,
    string message)
{
    var normalizedMessage =
        message.Trim();

    if (string.IsNullOrWhiteSpace(
            normalizedMessage))
    {
        return;
    }

    if (!string.IsNullOrWhiteSpace(
            contribution.Note) &&
        contribution.Note.Contains(
            normalizedMessage,
            StringComparison.OrdinalIgnoreCase))
    {
        return;
    }

    contribution.Note =
        string.IsNullOrWhiteSpace(
            contribution.Note)
            ? normalizedMessage
            : contribution.Note.Trim() +
              " | " +
              normalizedMessage;
}

/// <summary>
/// Liên kết và khấu trừ BHXH khi chốt bảng lương.
///
/// Quy tắc:
/// - Chỉ FULL_TIME tham gia BHXH.
/// - Nếu lương đủ, khấu trừ toàn bộ phần nhân viên đóng.
/// - Nếu lương không đủ, khấu trừ tối đa số tiền hiện có.
/// - Phần còn thiếu được ghi nhận là doanh nghiệp tạm ứng.
/// - Bảng lương vẫn được chốt và khoản đóng chuyển CONFIRMED.
/// - Admin chỉ chuyển CONFIRMED sang PAID sau khi doanh nghiệp
///   thực tế nộp khoản BHXH.
/// </summary>


/// <summary>
/// Khấu trừ BHXH khi chốt bảng lương.
///
/// Thứ tự thực hiện:
/// 1. Khấu trừ khoản BHXH của tháng hiện tại.
/// 2. Dùng phần lương còn lại để thu hồi các khoản
///    doanh nghiệp đã ứng trước ở những tháng cũ.
/// 3. Ưu tiên thu hồi khoản cũ nhất.
/// </summary>
private async Task
    ApplySocialInsuranceDeductionAsync(
        LuongMonthlySalary salary)
{
    if (salary.User == null)
    {
        throw new InvalidOperationException(
            "Không xác định được nhân viên " +
            "của bảng lương.");
    }

    // Hàm này chỉ được gọi đối với bảng lương PENDING.
    // Việc đặt lại các giá trị giúp tránh sử dụng
    // dữ liệu tạm của lần tính trước.
    salary.CurrentBhxhDeduction =
        0;

    salary.PreviousBhxhRecovery =
        0;

    salary.SocialInsuranceDeduction =
        0;

    var availableSalary =
        Math.Max(
            0m,
            salary.TotalSalary);

    var vietnamNow =
        DateTime.UtcNow.AddHours(7);

    // =====================================================
    // 1. KHẤU TRỪ BHXH CỦA THÁNG HIỆN TẠI
    // =====================================================

    if (SalaryWagePolicy
        .IsSocialInsuranceEligible(
            salary.User.EmploymentType))
    {
        var currentContribution =
            await GetOrCreateSocialInsuranceContributionAsync(
                salary);

        var currentStatus =
            (currentContribution.Status ?? "DRAFT")
                .Trim()
                .ToUpperInvariant();

        if (currentStatus == "PAID")
        {
            throw new InvalidOperationException(
                "Khoản BHXH của tháng hiện tại đã được " +
                "xác nhận nộp nhưng bảng lương chưa chốt. " +
                "Vui lòng kiểm tra lại dữ liệu.");
        }

        if (currentStatus != "CANCELLED")
        {
            salary.BhxhContribution =
                currentContribution;

            // Nếu khoản mới được tạo thì ID hiện tại có thể
            // bằng 0. EF Core sẽ tự cập nhật khóa ngoại khi lưu.
            if (currentContribution.Id > 0)
            {
                salary.BhxhContributionId =
                    currentContribution.Id;
            }

            var currentOutstanding =
                Math.Max(
                    0m,
                    currentContribution
                        .EmployeeOutstandingAmount);

            var currentDeduction =
                Math.Min(
                    availableSalary,
                    currentOutstanding);

            currentContribution
                .EmployeeDeductedAmount +=
                    currentDeduction;

            currentContribution
                .EmployeeOutstandingAmount -=
                    currentDeduction;

            UpdateDeductionStatus(
                currentContribution);

            // Khoản phải đóng đã được xác định khi
            // bảng lương được chốt.
            currentContribution.Status =
                "CONFIRMED";

            currentContribution.ConfirmedAt =
                vietnamNow;

            currentContribution.UpdatedAt =
                vietnamNow;

            salary.CurrentBhxhDeduction =
                currentDeduction;

            availableSalary -=
                currentDeduction;

            if (currentContribution
                    .EmployeeOutstandingAmount > 0)
            {
                AppendContributionNote(
                    currentContribution,
                    $"Đã khấu trừ " +
                    $"{currentDeduction:N0} đồng trong kỳ " +
                    $"{salary.Month}/{salary.Year}; " +
                    $"doanh nghiệp ứng trước phần còn thiếu " +
                    $"{currentContribution.EmployeeOutstandingAmount:N0} đồng.");
            }
            else
            {
                AppendContributionNote(
                    currentContribution,
                    $"Đã khấu trừ đủ phần nhân viên đóng " +
                    $"trong kỳ {salary.Month}/{salary.Year}.");
            }
        }
        else
        {
            // Khoản của tháng đã bị hủy hợp lệ.
            salary.BhxhContribution =
                currentContribution;

            if (currentContribution.Id > 0)
            {
                salary.BhxhContributionId =
                    currentContribution.Id;
            }
        }
    }
    else
    {
        salary.BhxhContribution =
            null;

        salary.BhxhContributionId =
            null;
    }

    // =====================================================
    // 2. THU HỒI CÁC KHOẢN DOANH NGHIỆP ĐÃ ỨNG TRƯỚC
    // =====================================================

    if (availableSalary > 0)
    {
        var previousContributions =
            await _context
                .BhxhMonthlyContributions
                .Where(item =>
                    item.UserId ==
                        salary.UserId &&

                    // Chỉ thu hồi khoản đã được xử lý
                    // trong một bảng lương trước đó.
                    (
                        item.Status == "CONFIRMED" ||
                        item.Status == "PAID"
                    ) &&

                    item.EmployeeOutstandingAmount >
                        0 &&

                    (
                        item.Year < salary.Year ||
                        (
                            item.Year == salary.Year &&
                            item.Month < salary.Month
                        )
                    ))
                .OrderBy(item =>
                    item.Year)
                .ThenBy(item =>
                    item.Month)
                .ThenBy(item =>
                    item.Id)
                .ToListAsync();

        foreach (var previousContribution
                 in previousContributions)
        {
            if (availableSalary <= 0)
            {
                break;
            }

            var previousOutstanding =
                Math.Max(
                    0m,
                    previousContribution
                        .EmployeeOutstandingAmount);

            var recoveryAmount =
                Math.Min(
                    availableSalary,
                    previousOutstanding);

            if (recoveryAmount <= 0)
            {
                continue;
            }

            previousContribution
                .EmployeeDeductedAmount +=
                    recoveryAmount;

            previousContribution
                .EmployeeOutstandingAmount -=
                    recoveryAmount;

            UpdateDeductionStatus(
                previousContribution);

            previousContribution.UpdatedAt =
                vietnamNow;

            // Trạng thái PAID hoặc CONFIRMED của khoản cũ
            // không bị thay đổi. Việc thu hồi chỉ liên quan
            // đến số tiền doanh nghiệp đã ứng cho Staff.
            var recovery =
                new BhxhDeductionRecovery
                {
                    UserId =
                        salary.UserId,

                    SourceContribution =
                        previousContribution,

                    RecoverySalary =
                        salary,

                    RecoveryAmount =
                        recoveryAmount,

                    Note =
                        $"Thu hồi khoản doanh nghiệp " +
                        $"đã ứng trước kỳ " +
                        $"{previousContribution.Month}/" +
                        $"{previousContribution.Year}.",

                    CreatedAt =
                        vietnamNow
                };

            await _context
                .BhxhDeductionRecoveries
                .AddAsync(recovery);

            salary.PreviousBhxhRecovery +=
                recoveryAmount;

            availableSalary -=
                recoveryAmount;

            AppendContributionNote(
                previousContribution,
                $"Đã thu hồi {recoveryAmount:N0} đồng " +
                $"qua bảng lương kỳ " +
                $"{salary.Month}/{salary.Year}. " +
                $"Còn phải thu hồi " +
                $"{previousContribution.EmployeeOutstandingAmount:N0} đồng.");
        }
    }

    // Tổng khấu trừ trên bảng lương gồm:
    // - BHXH của tháng hiện tại.
    // - Khoản thu hồi tạm ứng của các tháng trước.
    salary.SocialInsuranceDeduction =
        salary.CurrentBhxhDeduction +
        salary.PreviousBhxhRecovery;
}

/// <summary>
/// Cập nhật trạng thái khấu trừ phần nhân viên đóng.
///
/// NONE:
/// Chưa khấu trừ được.
///
/// PARTIAL:
/// Đã khấu trừ hoặc thu hồi được một phần.
///
/// FULL:
/// Đã khấu trừ và thu hồi đầy đủ.
/// </summary>
private static void UpdateDeductionStatus(
    BhxhMonthlyContribution contribution)
{
    contribution.EmployeeDeductedAmount =
        Math.Max(
            0m,
            contribution.EmployeeDeductedAmount);

    contribution.EmployeeOutstandingAmount =
        Math.Max(
            0m,
            contribution.EmployeeOutstandingAmount);

    if (contribution.EmployeeOutstandingAmount <= 0)
    {
        contribution.EmployeeOutstandingAmount =
            0;

        contribution.DeductionStatus =
            "FULL";

        return;
    }

    contribution.DeductionStatus =
        contribution.EmployeeDeductedAmount > 0
            ? "PARTIAL"
            : "NONE";
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

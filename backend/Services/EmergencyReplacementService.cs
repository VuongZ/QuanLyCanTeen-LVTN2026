using System.Globalization;
using System.Text;
using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Services;

public class EmergencyReplacementService
{
    private const string PublishedStatus =
        "PUBLISHED";

    private const string LeaveApprovedStatus =
        "LEAVE_APPROVED";

    private const string AbsentStatus =
        "ABSENT";

    private const string WaitlistStatus =
        "WAITLIST";

    private const string ReplacementSelectedStatus =
        "REPLACEMENT_SELECTED";

    private const string EmergencyReplacementType =
        "EMERGENCY_REPLACEMENT";

    private const decimal DefaultReplacementMultiplier =
        1.50m;

    // Sau giờ bắt đầu 15 phút mới được ghi nhận
    // vắng không phép.
    private const int AbsenceGraceMinutes = 15;

    private readonly EmergencyReplacementRepo _repo;

    public EmergencyReplacementService(
        EmergencyReplacementRepo repo)
    {
        _repo = repo;
    }

    private static TimeZoneInfo
        GetVietnamTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(
                "SE Asia Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById(
                "Asia/Ho_Chi_Minh");
        }
    }

    private static DateTime GetVietnamNow()
    {
        var vietnamNow =
            TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                GetVietnamTimeZone());

        return DateTime.SpecifyKind(
            vietnamNow,
            DateTimeKind.Unspecified);
    }

    private static string NormalizeText(
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized =
            value.Normalize(
                NormalizationForm.FormD);

        var builder =
            new StringBuilder();

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo
                    .GetUnicodeCategory(character) !=
                UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder
            .ToString()
            .Normalize(
                NormalizationForm.FormC)
            .ToUpperInvariant();
    }

    /// <summary>
    /// Manager ghi nhận Staff nghỉ có phép.
    /// </summary>
    public async Task<object>
        MarkApprovedLeaveAsync(
            int scheduleId,
            int managerId,
            ScheduleAbsenceDto dto)
    {
        return await MarkAbsenceAsync(
            scheduleId,
            managerId,
            dto,
            LeaveApprovedStatus,
            requireGracePeriod: false);
    }

    /// <summary>
    /// Manager ghi nhận Staff vắng không phép.
    ///
    /// Chỉ được thực hiện sau giờ bắt đầu ca
    /// cộng thời gian chờ 15 phút.
    /// </summary>
    public async Task<object>
        MarkAbsentAsync(
            int scheduleId,
            int managerId,
            ScheduleAbsenceDto dto)
    {
        return await MarkAbsenceAsync(
            scheduleId,
            managerId,
            dto,
            AbsentStatus,
            requireGracePeriod: true);
    }

    private async Task<object>
        MarkAbsenceAsync(
            int scheduleId,
            int managerId,
            ScheduleAbsenceDto dto,
            string targetStatus,
            bool requireGracePeriod)
    {
        var reason =
            dto.Reason?.Trim();

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException(
                "Vui lòng nhập lý do nghỉ hoặc vắng.");
        }

        if (reason.Length > 500)
        {
            throw new ArgumentException(
                "Lý do không được vượt quá 500 ký tự.");
        }

        await using var transaction =
            await _repo
                .BeginSerializableTransactionAsync();

        try
        {
            var schedule =
                await _repo.GetScheduleForUpdateAsync(
                    scheduleId);

            if (schedule == null)
            {
                throw new KeyNotFoundException(
                    "Không tìm thấy lịch làm cần xử lý.");
            }

            var branchId =
                await ValidateManagerAndBranchAsync(
                    managerId,
                    schedule);

            ValidateTargetEmployee(
                schedule);

            if (!string.Equals(
                    schedule.Status,
                    PublishedStatus,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Chỉ được ghi nhận nghỉ hoặc vắng " +
                    "đối với lịch đang được công bố.");
            }

            if (schedule.CaAttendances.Any(
                    attendance =>
                        attendance.CheckInTime != null))
            {
                throw new InvalidOperationException(
                    "Nhân viên đã check-in nên không thể " +
                    "ghi nhận nghỉ hoặc vắng cho lịch này.");
            }

            if (schedule.ReplacementSchedule != null ||
                await _repo.HasReplacementAsync(
                    schedule.Id))
            {
                throw new InvalidOperationException(
                    "Lịch này đã có người được điều động thay.");
            }

            var vietnamNow =
                GetVietnamNow();

            if (requireGracePeriod)
            {
                var shiftStart =
                    schedule.WorkDate.ToDateTime(
                        schedule.Shift.StartTime);

                var allowedAbsentTime =
                    shiftStart.AddMinutes(
                        AbsenceGraceMinutes);

                if (vietnamNow <
                    allowedAbsentTime)
                {
                    throw new InvalidOperationException(
                        "Chưa thể ghi nhận vắng không phép. " +
                        $"Vui lòng chờ đến " +
                        $"{allowedAbsentTime:dd/MM/yyyy HH:mm}.");
                }
            }

            schedule.Status =
                targetStatus;

            schedule.AbsenceReason =
                reason;

            schedule.AbsenceMarkedByUserId =
                managerId;

            schedule.AbsenceMarkedAt =
                vietnamNow;

            await _repo.SaveChangesAsync();

            await transaction.CommitAsync();

            return new
            {
                scheduleId =
                    schedule.Id,

                userId =
                    schedule.UserId,

                employeeName =
                    schedule.User.FullName,

                branchId,

                schedule.Status,

                absenceReason =
                    schedule.AbsenceReason,

                absenceMarkedAt =
                    schedule.AbsenceMarkedAt
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Lấy danh sách WAITLIST có thể thay cho lịch
    /// đã được đánh dấu nghỉ hoặc vắng.
    /// </summary>
    public async Task<List<ReplacementCandidateDto>>
        GetReplacementCandidatesAsync(
            int scheduleId,
            int managerId)
    {
        var originalSchedule =
            await _repo.GetScheduleForUpdateAsync(
                scheduleId);

        if (originalSchedule == null)
        {
            throw new KeyNotFoundException(
                "Không tìm thấy lịch cần thay thế.");
        }

        var branchId =
            await ValidateManagerAndBranchAsync(
                managerId,
                originalSchedule);

        ValidateTargetEmployee(
            originalSchedule);

        ValidateReplacementSourceStatus(
            originalSchedule);

        if (originalSchedule.PeriodId is not int periodId)
        {
            throw new InvalidOperationException(
                "Lịch này chưa được liên kết với đợt " +
                "đăng ký nên không thể lấy danh sách chờ.");
        }

        if (originalSchedule.ReplacementSchedule != null ||
            await _repo.HasReplacementAsync(
                originalSchedule.Id))
        {
            throw new InvalidOperationException(
                "Lịch này đã có người được chọn thay.");
        }

        var waitlist =
            await _repo.GetWaitlistCandidatesAsync(
                periodId,
                originalSchedule.ShiftId,
                originalSchedule.WorkDate,
                branchId);

        var candidateUserIds =
            waitlist
                .Select(registration =>
                    registration.UserId)
                .Distinct()
                .ToList();

        var publishedSchedules =
            await _repo
                .GetPublishedSchedulesForUsersOnDateAsync(
                    candidateUserIds,
                    originalSchedule.WorkDate);

        var schedulesByUser =
            publishedSchedules
                .GroupBy(schedule =>
                    schedule.UserId)
                .ToDictionary(
                    group => group.Key,
                    group => group.ToList());

        var result =
            new List<ReplacementCandidateDto>();

        foreach (var registration in waitlist)
        {
            if (registration.UserId ==
                originalSchedule.UserId)
            {
                continue;
            }

            if (!IsStaffRole(
                    registration.User.Role?.RoleName))
            {
                continue;
            }

            var hasConflict = false;

            if (schedulesByUser.TryGetValue(
                    registration.UserId,
                    out var userSchedules))
            {
                hasConflict =
                    userSchedules.Any(schedule =>
                        TimesOverlap(
                            originalSchedule
                                .Shift.StartTime,
                            originalSchedule
                                .Shift.EndTime,
                            schedule.Shift.StartTime,
                            schedule.Shift.EndTime));
            }

            if (hasConflict)
            {
                continue;
            }

            result.Add(
                new ReplacementCandidateDto
                {
                    RegistrationId =
                        registration.Id,

                    UserId =
                        registration.UserId,

                    FullName =
                        registration.User.FullName,

                    PhoneNumber =
                        registration.User.PhoneNumber,

                    Email =
                        registration.User.Email,

                    RoleName =
                        registration.User.Role?
                            .RoleName,

                    RegisteredAt =
                        registration.RegisteredAt,

                    QueuePosition =
                        result.Count + 1
                });
        }

        return result;
    }

    /// <summary>
    /// Manager xác nhận chọn một Staff trong WAITLIST
    /// để thay ca khẩn cấp.
    /// </summary>
    public async Task<object>
        AssignEmergencyReplacementAsync(
            int originalScheduleId,
            int managerId,
            EmergencyReplacementDto dto)
    {
        await using var transaction =
            await _repo
                .BeginSerializableTransactionAsync();

        try
        {
            var originalSchedule =
                await _repo.GetScheduleForUpdateAsync(
                    originalScheduleId);

            if (originalSchedule == null)
            {
                throw new KeyNotFoundException(
                    "Không tìm thấy lịch cần thay thế.");
            }

            var branchId =
                await ValidateManagerAndBranchAsync(
                    managerId,
                    originalSchedule);

            ValidateTargetEmployee(
                originalSchedule);

            ValidateReplacementSourceStatus(
                originalSchedule);

            if (originalSchedule.CaAttendances.Any(
                    attendance =>
                        attendance.CheckInTime != null))
            {
                throw new InvalidOperationException(
                    "Nhân viên ban đầu đã check-in, " +
                    "không thể điều động người thay.");
            }

            if (originalSchedule.PeriodId is not int periodId)
            {
                throw new InvalidOperationException(
                    "Lịch này chưa được liên kết với " +
                    "đợt đăng ký.");
            }

            if (originalSchedule.ReplacementSchedule != null ||
                await _repo.HasReplacementAsync(
                    originalSchedule.Id))
            {
                throw new InvalidOperationException(
                    "Lịch này đã có người được chọn thay.");
            }

            var replacementRegistration =
                await _repo.GetRegistrationForUpdateAsync(
                    dto.ReplacementRegistrationId);

            if (replacementRegistration == null)
            {
                throw new KeyNotFoundException(
                    "Không tìm thấy phiếu đăng ký dự phòng.");
            }

            if (!string.Equals(
                    replacementRegistration.Status,
                    WaitlistStatus,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Phiếu đăng ký này không còn nằm " +
                    "trong danh sách chờ.");
            }

            if (replacementRegistration.PeriodId !=
                    periodId ||
                replacementRegistration.ShiftId !=
                    originalSchedule.ShiftId ||
                replacementRegistration.WorkDate !=
                    originalSchedule.WorkDate)
            {
                throw new InvalidOperationException(
                    "Người được chọn không thuộc đúng đợt, " +
                    "ngày và ca cần thay.");
            }

            if (replacementRegistration.UserId ==
                originalSchedule.UserId)
            {
                throw new InvalidOperationException(
                    "Không thể chọn chính nhân viên đang nghỉ.");
            }

            if (replacementRegistration.User.BranchId !=
                branchId)
            {
                throw new InvalidOperationException(
                    "Người được chọn không thuộc cùng chi nhánh.");
            }

            if (!IsStaffRole(
                    replacementRegistration
                        .User.Role?.RoleName))
            {
                throw new InvalidOperationException(
                    "Chỉ được chọn tài khoản Nhân viên " +
                    "để thay ca.");
            }

            var candidateSchedules =
                await _repo
                    .GetPublishedSchedulesForUsersOnDateAsync(
                        new List<int>
                        {
                            replacementRegistration.UserId
                        },
                        originalSchedule.WorkDate);

            var hasTimeConflict =
                candidateSchedules.Any(schedule =>
                    TimesOverlap(
                        originalSchedule.Shift.StartTime,
                        originalSchedule.Shift.EndTime,
                        schedule.Shift.StartTime,
                        schedule.Shift.EndTime));

            if (hasTimeConflict)
            {
                throw new InvalidOperationException(
                    "Nhân viên được chọn đang có một lịch " +
                    "khác bị trùng thời gian.");
            }

            var salaryRule =
                await _repo.GetSalaryRuleByBranchAsync(
                    branchId);

            var replacementMultiplier =
                salaryRule != null &&
                salaryRule
                    .EmergencyReplacementMultiplier > 0
                    ? salaryRule
                        .EmergencyReplacementMultiplier
                    : DefaultReplacementMultiplier;

            replacementRegistration.Status =
                ReplacementSelectedStatus;

            var vietnamNow =
                GetVietnamNow();

            var replacementSchedule =
                new CaFinalSchedule
                {
                    PeriodId =
                        periodId,

                    SourceRegistrationId =
                        replacementRegistration.Id,

                    UserId =
                        replacementRegistration.UserId,

                    ShiftId =
                        originalSchedule.ShiftId,

                    WorkDate =
                        originalSchedule.WorkDate,

                    Status =
                        PublishedStatus,

                    AssignmentType =
                        EmergencyReplacementType,

                    PayMultiplier =
                        replacementMultiplier,

                    ReplacesScheduleId =
                        originalSchedule.Id,

                    AbsenceReason =
                        null,

                    AbsenceMarkedByUserId =
                        null,

                    AbsenceMarkedAt =
                        null,

                    AssignedByUserId =
                        managerId,

                    AssignedAt =
                        vietnamNow
                };

            _repo.AddSchedule(
                replacementSchedule);

            await _repo.SaveChangesAsync();

            await transaction.CommitAsync();

            return new
            {
                originalScheduleId =
                    originalSchedule.Id,

                originalEmployee = new
                {
                    id =
                        originalSchedule.UserId,

                    fullName =
                        originalSchedule.User.FullName,

                    status =
                        originalSchedule.Status
                },

                replacementScheduleId =
                    replacementSchedule.Id,

                replacementEmployee = new
                {
                    id =
                        replacementRegistration.UserId,

                    fullName =
                        replacementRegistration
                            .User.FullName,

                    phoneNumber =
                        replacementRegistration
                            .User.PhoneNumber,

                    email =
                        replacementRegistration
                            .User.Email
                },

                registrationId =
                    replacementRegistration.Id,

                registrationStatus =
                    replacementRegistration.Status,

                assignmentType =
                    replacementSchedule.AssignmentType,

                payMultiplier =
                    replacementSchedule.PayMultiplier,

                assignedAt =
                    replacementSchedule.AssignedAt
            };
        }
        catch (DbUpdateException ex)
        {
            await transaction.RollbackAsync();

            throw new InvalidOperationException(
                "Dữ liệu thay ca vừa được cập nhật bởi " +
                "một thao tác khác. Vui lòng tải lại và thử lại.",
                ex);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task<int>
        ValidateManagerAndBranchAsync(
            int managerId,
            CaFinalSchedule schedule)
    {
        var actor =
            await _repo.GetActorAsync(
                managerId);

        if (actor == null)
        {
            throw new KeyNotFoundException(
                "Không tìm thấy người thực hiện thao tác.");
        }

        var normalizedRole =
            NormalizeText(
                actor.Role?.RoleName);

        var isManager =
            normalizedRole.Contains("MANAGER") ||
            normalizedRole.Contains("QUAN LY");

        if (!isManager)
        {
            throw new InvalidOperationException(
                "Chỉ Quản lý mới được xử lý thay ca.");
        }

        if (actor.BranchId is not int actorBranchId)
        {
            throw new InvalidOperationException(
                "Quản lý chưa được gán chi nhánh.");
        }

        if (schedule.Shift.BranchId is not int
                scheduleBranchId ||
            scheduleBranchId != actorBranchId)
        {
            throw new InvalidOperationException(
                "Bạn không được xử lý lịch của " +
                "chi nhánh khác.");
        }

        return actorBranchId;
    }

    private static void ValidateTargetEmployee(
        CaFinalSchedule schedule)
    {
        if (!IsStaffRole(
                schedule.User.Role?.RoleName))
        {
            throw new InvalidOperationException(
                "Chỉ được thực hiện nghiệp vụ thay ca " +
                "đối với lịch của Nhân viên.");
        }
    }

    private static bool IsStaffRole(
        string? roleName)
    {
        var normalizedRole =
            NormalizeText(
                roleName);

        if (string.IsNullOrWhiteSpace(
                normalizedRole))
        {
            return false;
        }

        return
            !normalizedRole.Contains("ADMIN") &&
            !normalizedRole.Contains("MANAGER") &&
            !normalizedRole.Contains("QUAN LY");
    }

    private static void
        ValidateReplacementSourceStatus(
            CaFinalSchedule schedule)
    {
        var isApprovedLeave =
            string.Equals(
                schedule.Status,
                LeaveApprovedStatus,
                StringComparison.OrdinalIgnoreCase);

        var isAbsent =
            string.Equals(
                schedule.Status,
                AbsentStatus,
                StringComparison.OrdinalIgnoreCase);

        if (!isApprovedLeave &&
            !isAbsent)
        {
            throw new InvalidOperationException(
                "Phải ghi nhận nhân viên nghỉ có phép " +
                "hoặc vắng không phép trước khi chọn người thay.");
        }
    }

    /// <summary>
    /// Kiểm tra hai khoảng thời gian ca có giao nhau.
    ///
    /// Có hỗ trợ ca đi qua 0 giờ.
    /// </summary>
    private static bool TimesOverlap(
        TimeOnly firstStart,
        TimeOnly firstEnd,
        TimeOnly secondStart,
        TimeOnly secondEnd)
    {
        const double minutesPerDay =
            24 * 60;

        var firstStartMinutes =
            firstStart.ToTimeSpan().TotalMinutes;

        var firstEndMinutes =
            firstEnd.ToTimeSpan().TotalMinutes;

        if (firstEndMinutes <=
            firstStartMinutes)
        {
            firstEndMinutes +=
                minutesPerDay;
        }

        var secondStartMinutes =
            secondStart.ToTimeSpan().TotalMinutes;

        var secondEndMinutes =
            secondEnd.ToTimeSpan().TotalMinutes;

        if (secondEndMinutes <=
            secondStartMinutes)
        {
            secondEndMinutes +=
                minutesPerDay;
        }

        var offsets = new[]
        {
            -minutesPerDay,
            0,
            minutesPerDay
        };

        return offsets.Any(offset =>
            firstStartMinutes <
                secondEndMinutes + offset &&
            secondStartMinutes + offset <
                firstEndMinutes);
    }
}
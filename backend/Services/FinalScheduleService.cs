using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Repositories;

namespace LuanVanTotNghiep.Services;

public class FinalScheduleService
{
    private const string PublishedStatus =
        "PUBLISHED";

    private const string DraftStatus =
        "DRAFT";

    private const string NormalAssignment =
        "NORMAL";

    private const decimal NormalPayMultiplier =
        1.00m;

    private readonly FinalScheduleRepo _repo;

    public FinalScheduleService(
        FinalScheduleRepo repo)
    {
        _repo = repo;
    }

    /// <summary>
    /// Lấy lịch làm chính thức của một đợt.
    /// </summary>
    public async Task<object>
        GetFinalSchedulesByPeriodAsync(
            int periodId)
    {
        var period =
            await _repo.GetPeriodAsNoTrackingAsync(
                periodId);

        if (period == null)
        {
            throw new KeyNotFoundException(
                "Không tìm thấy đợt đăng ký.");
        }

        if (!string.Equals(
                period.Status,
                PublishedStatus,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Đợt đăng ký chưa được công bố lịch chính thức.");
        }

        var schedules =
            await _repo
                .GetPublishedSchedulesByPeriodAsync(
                    period);

        return schedules
            .Select(schedule => new
            {
                id = schedule.Id,

                periodId =
                    schedule.PeriodId,

                sourceRegistrationId =
                    schedule.SourceRegistrationId,

                userId =
                    schedule.UserId,

                shiftId =
                    schedule.ShiftId,

                workDate =
                    schedule.WorkDate,

                status =
                    schedule.Status,

                assignmentType =
                    schedule.AssignmentType,

                payMultiplier =
                    schedule.PayMultiplier,

                replacesScheduleId =
                    schedule.ReplacesScheduleId,

                absenceReason =
                    schedule.AbsenceReason,

                user = new
                {
                    id =
                        schedule.User.Id,

                    fullName =
                        schedule.User.FullName,

                    email =
                        schedule.User.Email,

                    phoneNumber =
                        schedule.User.PhoneNumber,

                    roleName =
                        schedule.User.Role != null
                            ? schedule.User.Role.RoleName
                            : null
                },

                shift = new
                {
                    id =
                        schedule.Shift.Id,

                    shiftName =
                        schedule.Shift.ShiftName,

                    startTime =
                        schedule.Shift.StartTime,

                    endTime =
                        schedule.Shift.EndTime,

                    branchId =
                        schedule.Shift.BranchId
                }
            })
            .ToList();
    }

    /// <summary>
    /// Manager công bố lịch làm chính thức.
    ///
    /// Chỉ các phiếu REGISTERED được đưa vào lịch.
    /// WAITLIST tiếp tục nằm trong danh sách dự phòng.
    /// </summary>
    public async Task PublishScheduleAsync(
        PublishScheduleDto dto)
    {
        await using var transaction =
            await _repo
                .BeginSerializableTransactionAsync();

        try
        {
            var period =
                await _repo.GetPeriodByIdAsync(
                    dto.PeriodId);

            if (period == null)
            {
                throw new KeyNotFoundException(
                    "Không tìm thấy đợt đăng ký này.");
            }

            if (string.Equals(
                    period.Status,
                    PublishedStatus,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Lịch đã được công bố, " +
                    "không thể công bố lại.");
            }

            if (!string.Equals(
                    period.Status,
                    "CLOSED",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Chỉ được công bố lịch khi " +
                    "đợt đăng ký đã được khóa.");
            }

            // 1. Lấy Manager của chi nhánh.
            var manager =
                await _repo.GetBranchManagerAsync(
                    period.BranchId);

            if (manager == null)
            {
                throw new InvalidOperationException(
                    "Không tìm thấy Quản lý của chi nhánh " +
                    "để thêm vào lịch làm.");
            }

            // 2. Chỉ lấy phiếu REGISTERED.
            // WAITLIST tuyệt đối không được công bố.
            var registrations =
                await _repo
                    .GetRegisteredRegistrationsAsync(
                        period.Id);

            // 3. Lấy các ca thuộc chi nhánh.
            var branchShifts =
                await _repo.GetBranchShiftsAsync(
                    period.BranchId);

            if (branchShifts.Count == 0)
            {
                throw new InvalidOperationException(
                    "Chi nhánh chưa có ca làm để công bố.");
            }

            var branchShiftIds =
                branchShifts
                    .Select(shift =>
                        shift.Id)
                    .ToList();

            var branchShiftIdSet =
                branchShiftIds.ToHashSet();

            // 4. Kiểm tra phiếu REGISTERED có đúng
            // chi nhánh và đúng thời gian đợt không.
            foreach (var registration in registrations)
            {
                if (!branchShiftIdSet.Contains(
                        registration.ShiftId))
                {
                    throw new InvalidOperationException(
                        $"Phiếu đăng ký #{registration.Id} " +
                        "không thuộc ca của chi nhánh.");
                }

                if (registration.WorkDate <
                        period.StartDate ||
                    registration.WorkDate >
                        period.EndDate)
                {
                    throw new InvalidOperationException(
                        $"Phiếu đăng ký #{registration.Id} " +
                        "có ngày làm ngoài phạm vi của đợt.");
                }
            }

            // 5. Lấy cấu hình hoạt động của ca.
            var shiftConfigs =
                await _repo.GetShiftConfigsAsync(
                    branchShiftIds);

            /*
             * Dictionary chứa toàn bộ lịch cần được công bố.
             *
             * Key:
             *   UserId + ShiftId + WorkDate
             *
             * Value:
             *   SourceRegistrationId
             *
             * Lịch Staff:
             *   SourceRegistrationId = registration.Id
             *
             * Lịch Manager:
             *   SourceRegistrationId = null
             */
            var desiredSchedules =
                new Dictionary<
                    (
                        int UserId,
                        int ShiftId,
                        DateOnly WorkDate
                    ),
                    int?>();

            // 6. Thêm các Staff có trạng thái REGISTERED.
            foreach (var registration in registrations)
            {
                var key =
                    (
                        registration.UserId,
                        registration.ShiftId,
                        registration.WorkDate
                    );

                if (!desiredSchedules.TryAdd(
                        key,
                        registration.Id))
                {
                    throw new InvalidOperationException(
                        "Phát hiện nhiều phiếu REGISTERED " +
                        "trùng người, ca và ngày làm.");
                }
            }

            // 7. Tự động thêm Manager vào tất cả
            // ca đang hoạt động trong đợt.
            var currentDate =
                period.StartDate;

            while (currentDate <= period.EndDate)
            {
                var dayOfWeek =
                    currentDate.DayOfWeek.ToString();

                foreach (var shift in branchShifts)
                {
                    var config =
                        shiftConfigs.FirstOrDefault(item =>
                            item.ShiftId == shift.Id &&
                            item.DayOfWeek == dayOfWeek);

                    if (config == null ||
                        config.MaxStaff
                            .GetValueOrDefault() <= 0)
                    {
                        continue;
                    }

                    var managerKey =
                        (
                            manager.Id,
                            shift.Id,
                            currentDate
                        );

                    // Manager không có phiếu đăng ký nguồn.
                    desiredSchedules.TryAdd(
                        managerKey,
                        null);
                }

                currentDate =
                    currentDate.AddDays(1);
            }

            // 8. Lấy lịch cũ trong phạm vi của đợt.
            var existingSchedules =
                await _repo.GetExistingSchedulesAsync(
                    period.Id,
                    period.StartDate,
                    period.EndDate,
                    branchShiftIds);

            // 9. Đồng bộ các lịch đã tồn tại.
            foreach (var schedule in existingSchedules)
            {
                var key =
                    (
                        schedule.UserId,
                        schedule.ShiftId,
                        schedule.WorkDate
                    );

                if (desiredSchedules.TryGetValue(
                        key,
                        out var sourceRegistrationId))
                {
                    ApplyNormalPublishedSchedule(
                        schedule,
                        period.Id,
                        sourceRegistrationId);

                    continue;
                }

                if (schedule.CaAttendances.Any())
                {
                    // Không xóa lịch đã phát sinh chấm công.
                    // Chuyển về DRAFT để lịch không xuất hiện
                    // trong lịch chính thức đã công bố.
                    schedule.Status =
                        DraftStatus;
                }
                else
                {
                    _repo.RemoveSchedule(
                        schedule);
                }
            }

            // 10. Tạo các lịch chưa tồn tại.
            var existingKeys =
                existingSchedules
                    .Select(schedule =>
                        (
                            schedule.UserId,
                            schedule.ShiftId,
                            schedule.WorkDate
                        ))
                    .ToHashSet();

            foreach (var desired in desiredSchedules)
            {
                if (existingKeys.Contains(
                        desired.Key))
                {
                    continue;
                }

                var newSchedule =
                    new CaFinalSchedule
                    {
                        PeriodId =
                            period.Id,

                        SourceRegistrationId =
                            desired.Value,

                        UserId =
                            desired.Key.UserId,

                        ShiftId =
                            desired.Key.ShiftId,

                        WorkDate =
                            desired.Key.WorkDate,

                        Status =
                            PublishedStatus,

                        AssignmentType =
                            NormalAssignment,

                        PayMultiplier =
                            NormalPayMultiplier,

                        ReplacesScheduleId =
                            null,

                        AbsenceReason =
                            null,

                        AbsenceMarkedByUserId =
                            null,

                        AbsenceMarkedAt =
                            null,

                        AssignedByUserId =
                            null,

                        AssignedAt =
                            null
                    };

                _repo.AddSchedule(
                    newSchedule);
            }

            // 11. Đánh dấu đợt đã công bố.
            period.Status =
                PublishedStatus;

            await _repo.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Chuẩn hóa một lịch cũ thành lịch làm bình thường
    /// thuộc đợt đang công bố.
    /// </summary>
    private static void ApplyNormalPublishedSchedule(
        CaFinalSchedule schedule,
        int periodId,
        int? sourceRegistrationId)
    {
        schedule.PeriodId =
            periodId;

        schedule.SourceRegistrationId =
            sourceRegistrationId;

        schedule.Status =
            PublishedStatus;

        schedule.AssignmentType =
            NormalAssignment;

        schedule.PayMultiplier =
            NormalPayMultiplier;

        schedule.ReplacesScheduleId =
            null;

        schedule.AbsenceReason =
            null;

        schedule.AbsenceMarkedByUserId =
            null;

        schedule.AbsenceMarkedAt =
            null;

        schedule.AssignedByUserId =
            null;

        schedule.AssignedAt =
            null;
    }
}
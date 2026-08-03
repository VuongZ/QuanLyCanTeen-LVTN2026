using System.Globalization;
using System.Text;
using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Services;

public partial class EmergencyReplacementService
{
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
}


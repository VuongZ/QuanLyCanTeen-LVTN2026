using System.Globalization;
using System.Text;
using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Services;

public partial class EmergencyReplacementService
{
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
}


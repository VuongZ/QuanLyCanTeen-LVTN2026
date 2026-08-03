using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Repositories;

namespace LuanVanTotNghiep.Services;

public partial class FinalScheduleService
{
public async Task<object>
        GetAutomaticFullTimeStaffAsync(
            int branchId)
    {
        var staff =
            await _repo.GetBranchFullTimeStaffAsync(
                branchId);

        return staff.Select(user => new
        {
            id = user.Id,
            fullName = user.FullName,
            employmentType = user.EmploymentType
        }).ToList();
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
}


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
}


using System.Globalization;
using System.Text;
using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Services;

public partial class ShiftDelegationService
{
public async Task<object> MarkAttendanceStatusAsync(
        int actorId,
        MarkDelegatedAttendanceDto dto)
    {
        var actor = await GetActorAsync(actorId);
        if (actor.BranchId is not int branchId)
            throw new InvalidOperationException("Tài khoản chưa được gán chi nhánh.");

        var branchIsActive = await context.DmBranches
            .AsNoTracking()
            .AnyAsync(branch =>
                branch.Id == branchId &&
                branch.IsActive);

        var shiftIsActive = await context.CaShifts
            .AsNoTracking()
            .AnyAsync(shift =>
                shift.Id == dto.ShiftId &&
                shift.IsActive);

        if (!branchIsActive || !shiftIsActive)
        {
            throw new InvalidOperationException(
                "Cơ sở hoặc ca làm đã ngừng hoạt động nên không thể ghi nhận đi trễ hoặc vắng mặt mới.");
        }

        var actorRole = Normalize(actor.Role?.RoleName);
        var isManager = IsManager(actorRole);
        var hasDelegation = await HasActivePermissionAsync(
            actorId, branchId, dto.ShiftId, dto.WorkDate);
        if (!isManager && !hasDelegation)
            throw new InvalidOperationException(
                "Bạn không có quyền trưởng ca trong thời gian của ca này.");

        var status = Normalize(dto.Status);
        if (status != "LATE" && status != "ABSENT")
            throw new InvalidOperationException("Trạng thái phải là LATE hoặc ABSENT.");

        var noteValue = dto.Note?.Trim();
        if (noteValue?.Length > 500)
            throw new InvalidOperationException("Ghi chú không được vượt quá 500 ký tự.");

        var schedule = await context.CaFinalSchedules
            .Include(item => item.User)
            .Include(item => item.Shift)
            .FirstOrDefaultAsync(item =>
                item.UserId == dto.EmployeeId &&
                item.ShiftId == dto.ShiftId &&
                item.WorkDate == dto.WorkDate &&
                item.Status == "PUBLISHED");
        if (schedule == null || schedule.User.BranchId != branchId)
            throw new InvalidOperationException(
                "Nhân viên không có lịch chính thức trong ca này.");

        var attendance = await context.CaAttendances
            .FirstOrDefaultAsync(item => item.ScheduleId == schedule.Id);
        if (attendance == null)
        {
            attendance = new CaAttendance { ScheduleId = schedule.Id };
            context.CaAttendances.Add(attendance);
        }

        if (status == "ABSENT" && attendance.CheckInTime != null)
            throw new InvalidOperationException(
                "Nhân viên đã check-in nên không thể ghi nhận vắng mặt.");

        attendance.Status = status == "LATE"
            ? "Đi muộn"
            : "Vắng mặt";
        await context.SaveChangesAsync();

        var note = string.IsNullOrWhiteSpace(noteValue) ? "" : $" Lý do: {noteValue}";
        await LogActiveActionAsync(
            actorId,
            branchId,
            dto.ShiftId,
            dto.WorkDate,
            status == "LATE" ? "ATTENDANCE_MARKED_LATE" : "ATTENDANCE_MARKED_ABSENT",
            $"Ghi nhận {schedule.User.FullName}: {attendance.Status}.{note}");

        return new
        {
            attendance.Id,
            scheduleId = schedule.Id,
            employeeId = schedule.UserId,
            employeeName = schedule.User.FullName,
            attendance.Status
        };
    }
}

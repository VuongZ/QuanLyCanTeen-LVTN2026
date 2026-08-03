using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.backend.Models.Entities;

namespace LuanVanTotNghiep.Services;

public partial class AttendanceService
{
    private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

    public async Task<List<DailyAttendanceHistoryDto>> GetDailyHistoryAsync(
        int managerId,
        DateOnly workDate,
        int? shiftId)
    {
        var manager = await _repo.GetManagerByIdAsync(managerId)
            ?? throw new KeyNotFoundException("Không tìm thấy quản lý.");

        var managerRole = NormalizeText(manager.Role?.RoleName);
        if (!managerRole.Contains("MANAGER") &&
            !managerRole.Contains("QUAN LY"))
        {
            throw new UnauthorizedAccessException(
                "Chỉ Quản lý mới được xem lịch sử chấm công.");
        }

        if (manager.BranchId is not int branchId)
        {
            throw new InvalidOperationException(
                "Quản lý chưa được phân công chi nhánh.");
        }

        var attendances = await _repo.GetDailyHistoryAsync(
            branchId,
            workDate,
            shiftId);

        return attendances.Select(attendance =>
        {
            var usesLegacyUtc = UsesLegacyUtc(attendance);
            var checkIn = NormalizeHistoryTime(
                attendance.CheckInTime,
                usesLegacyUtc);
            var checkOut = NormalizeHistoryTime(
                attendance.CheckOutTime,
                usesLegacyUtc);
            var workedHours = checkIn.HasValue && checkOut.HasValue
                ? AttendanceWorkHourPolicy.CalculateCreditedHours(
                    attendance.Schedule.User,
                    attendance.Schedule,
                    checkIn.Value,
                    checkOut.Value)
                : 0;

            return new DailyAttendanceHistoryDto
            {
                AttendanceId = attendance.Id,
                EmployeeId = attendance.Schedule.UserId,
                EmployeeName =
                    attendance.Schedule.User.FullName
                    ?? attendance.Schedule.User.Email
                    ?? attendance.Schedule.User.PhoneNumber
                    ?? $"Nhân viên {attendance.Schedule.UserId}",
                RoleName = attendance.Schedule.User.Role?.RoleName,
                ShiftId = attendance.Schedule.ShiftId,
                ShiftName = attendance.Schedule.Shift.ShiftName,
                WorkDate = attendance.Schedule.WorkDate,
                CheckInTime = checkIn,
                CheckOutTime = checkOut,
                WorkedHours = workedHours,
                Status = attendance.Status ?? string.Empty
            };
        }).ToList();
    }

    private static bool UsesLegacyUtc(CaAttendance attendance)
    {
        var shift = attendance.Schedule.Shift;
        var expectedEnd = attendance.Schedule.WorkDate.ToDateTime(
            shift.EndTime);
        if (shift.EndTime < shift.StartTime)
            expectedEnd = expectedEnd.AddDays(1);

        var legacyEnd = expectedEnd.Subtract(VietnamOffset);
        return attendance.CheckoutRequests.Any(request =>
            Math.Abs(
                (request.ProposedCheckOutTime - legacyEnd).TotalMinutes) <= 1);
    }

    private static DateTime? NormalizeHistoryTime(
        DateTime? value,
        bool usesLegacyUtc)
    {
        if (!value.HasValue)
            return null;

        return DateTime.SpecifyKind(
            usesLegacyUtc
                ? value.Value.Add(VietnamOffset)
                : value.Value,
            DateTimeKind.Unspecified);
    }
}

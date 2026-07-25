using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Repositories;

namespace LuanVanTotNghiep.Services;

public class FinalScheduleService
{
    private readonly FinalScheduleRepo _repo;

    public FinalScheduleService(FinalScheduleRepo repo)
    {
        _repo = repo;
    }

    // Lấy lịch làm chính thức của một đợt.
    public async Task<object>
        GetFinalSchedulesByPeriodAsync(int periodId)
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
                "PUBLISHED",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Đợt đăng ký chưa được công bố lịch chính thức.");
        }

        var schedules =
            await _repo.GetPublishedSchedulesByPeriodAsync(
                period);

        return schedules
            .Select(schedule => new
            {
                id = schedule.Id,
                userId = schedule.UserId,
                shiftId = schedule.ShiftId,
                workDate = schedule.WorkDate,
                status = schedule.Status,

                user = new
                {
                    id = schedule.User.Id,
                    fullName = schedule.User.FullName,
                    email = schedule.User.Email,
                    roleName = schedule.User.Role != null
                        ? schedule.User.Role.RoleName
                        : null
                },

                shift = new
                {
                    id = schedule.Shift.Id,
                    shiftName = schedule.Shift.ShiftName,
                    startTime = schedule.Shift.StartTime,
                    endTime = schedule.Shift.EndTime,
                    branchId = schedule.Shift.BranchId
                }
            })
            .ToList();
    }

    // Manager công bố lịch làm chính thức.
    public async Task PublishScheduleAsync(
        PublishScheduleDto dto)
    {
        await using var transaction =
            await _repo.BeginSerializableTransactionAsync();

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
                    "PUBLISHED",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Lịch đã được công bố, không thể công bố lại.");
            }

            if (!string.Equals(
                    period.Status,
                    "CLOSED",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Chỉ được công bố lịch khi đợt đăng ký đã được khóa.");
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

            // 2. Lấy các đăng ký hợp lệ của Staff.
            var cancelledStatuses = new[]
            {
                "CANCELLED",
                "REJECTED",
                "Từ Chối"
            };

            var registrations =
                await _repo.GetValidRegistrationsAsync(
                    dto.PeriodId,
                    cancelledStatuses);

            // 3. Lấy các ca thuộc chi nhánh.
            var branchShifts =
                await _repo.GetBranchShiftsAsync(
                    period.BranchId);

            var branchShiftIds = branchShifts
                .Select(shift => shift.Id)
                .ToList();

            // 4. Lấy cấu hình ca theo ngày.
            var shiftConfigs =
                await _repo.GetShiftConfigsAsync(
                    branchShiftIds);

            // 5. Tạo tập lịch chính thức cần có.
            var finalScheduleKeys =
                new HashSet<(
                    int UserId,
                    int ShiftId,
                    DateOnly WorkDate)>();

            // Thêm Staff đã đăng ký hợp lệ.
            foreach (var registration in registrations)
            {
                finalScheduleKeys.Add((
                    registration.UserId,
                    registration.ShiftId,
                    registration.WorkDate));

                registration.Status = "REGISTERED";
            }

            // Tự động thêm Manager vào các ca hoạt động.
            var currentDate = period.StartDate;

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
                        config.MaxStaff <= 0)
                    {
                        continue;
                    }

                    finalScheduleKeys.Add((
                        manager.Id,
                        shift.Id,
                        currentDate));
                }

                currentDate = currentDate.AddDays(1);
            }

            // 6. Lấy lịch chính thức cũ.
            var existingSchedules =
                await _repo.GetExistingSchedulesAsync(
                    period.StartDate,
                    period.EndDate,
                    branchShiftIds);

            // 7. Cập nhật hoặc xóa lịch cũ.
            foreach (var schedule in existingSchedules)
            {
                var stillExists =
                    finalScheduleKeys.Contains((
                        schedule.UserId,
                        schedule.ShiftId,
                        schedule.WorkDate));

                if (stillExists)
                {
                    schedule.Status = "PUBLISHED";
                }
                else if (schedule.CaAttendances.Any())
                {
                    // Không xóa lịch đã phát sinh điểm danh.
                    schedule.Status = "DRAFT";
                }
                else
                {
                    _repo.RemoveSchedule(schedule);
                }
            }

            // 8. Thêm lịch mới chưa tồn tại.
            var existingKeys = existingSchedules
                .Select(schedule => (
                    schedule.UserId,
                    schedule.ShiftId,
                    schedule.WorkDate))
                .ToHashSet();

            foreach (var key in finalScheduleKeys)
            {
                if (existingKeys.Contains(key))
                {
                    continue;
                }

                _repo.AddSchedule(
                    new CaFinalSchedule
                    {
                        UserId = key.UserId,
                        ShiftId = key.ShiftId,
                        WorkDate = key.WorkDate,
                        Status = "PUBLISHED"
                    });
            }

            period.Status = "PUBLISHED";

            await _repo.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
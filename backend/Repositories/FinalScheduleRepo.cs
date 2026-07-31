using System.Data;
using LuanVanTotNghiep.backend.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace LuanVanTotNghiep.Repositories;

public class FinalScheduleRepo
    : Repository<CaFinalSchedule>
{
    public FinalScheduleRepo(AppDbContext context)
        : base(context)
    {
    }

    public override async Task<CaFinalSchedule?> GetbyId(
        int id)
    {
        return await _dbSet
            .FirstOrDefaultAsync(schedule =>
                schedule.Id == id);
    }

    public async Task<IDbContextTransaction>
        BeginSerializableTransactionAsync()
    {
        return await Context.Database
            .BeginTransactionAsync(
                IsolationLevel.Serializable);
    }

    /// <summary>
    /// Lấy đợt đăng ký chỉ để đọc.
    /// </summary>
    public async Task<CaSchedulePeriod?>
        GetPeriodAsNoTrackingAsync(
            int periodId)
    {
        return await Context.CaSchedulePeriods
            .AsNoTracking()
            .FirstOrDefaultAsync(period =>
                period.Id == periodId);
    }

    /// <summary>
    /// Lấy đợt đăng ký để cập nhật trạng thái.
    /// </summary>
    public async Task<CaSchedulePeriod?>
        GetPeriodByIdAsync(
            int periodId)
    {
        return await Context.CaSchedulePeriods
            .FirstOrDefaultAsync(period =>
                period.Id == periodId);
    }

    /// <summary>
/// Lấy các lịch cần hiển thị của một đợt.
///
/// Bao gồm:
/// - PUBLISHED: đang làm việc bình thường.
/// - LEAVE_APPROVED: nghỉ có phép.
/// - ABSENT: vắng không phép.
///
/// Lịch nghỉ hoặc vắng vẫn phải được giữ lại để
/// Manager biết ai là người được thay thế.
/// </summary>
public async Task<List<CaFinalSchedule>>
    GetPublishedSchedulesByPeriodAsync(
        CaSchedulePeriod period)
{
    var visibleStatuses = new[]
    {
        "PUBLISHED",
        "LEAVE_APPROVED",
        "ABSENT"
    };

    return await _dbSet
        .AsNoTracking()
        .Include(schedule =>
            schedule.User)
            .ThenInclude(user =>
                user.Role)
        .Include(schedule =>
            schedule.Shift)
        .Where(schedule =>
            visibleStatuses.Contains(
                schedule.Status) &&
            (
                schedule.PeriodId == period.Id ||
                (
                    schedule.PeriodId == null &&
                    schedule.WorkDate >=
                        period.StartDate &&
                    schedule.WorkDate <=
                        period.EndDate &&
                    schedule.Shift.BranchId ==
                        period.BranchId
                )
            ))
        .OrderBy(schedule =>
            schedule.WorkDate)
        .ThenBy(schedule =>
            schedule.Shift.StartTime)
        .ThenBy(schedule =>
            schedule.UserId)
        .ToListAsync();
}

    /// <summary>
    /// Lấy Manager thuộc chi nhánh.
    /// </summary>
    public async Task<NsUser?>
        GetBranchManagerAsync(
            int? branchId)
    {
        return await Context.NsUsers
            .AsNoTracking()
            .Include(user =>
                user.Role)
            .Where(user =>
                user.BranchId == branchId &&
                user.IsDeleted != true &&
                user.Role != null &&
                user.Role.RoleName != null)
            .Where(user =>
                user.Role!.RoleName!.Contains(
                    "Manager") ||
                user.Role.RoleName.Contains(
                    "MANAGER") ||
                user.Role.RoleName.Contains(
                    "Quản lý") ||
                user.Role.RoleName.Contains(
                    "Quan ly"))
            .OrderBy(user =>
                user.Id)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Chỉ lấy các phiếu đang giữ vị trí chính thức.
    ///
    /// WAITLIST không được lấy vào lịch công bố.
    /// </summary>
    public async Task<List<CaStaffRegistration>>
        GetRegisteredRegistrationsAsync(
            int periodId)
    {
        return await Context.CaStaffRegistrations
            .AsNoTracking()
            .Where(registration =>
                registration.PeriodId == periodId &&
                registration.Status == "REGISTERED")
            .OrderBy(registration =>
                registration.WorkDate)
            .ThenBy(registration =>
                registration.ShiftId)
            .ThenBy(registration =>
                registration.RegisteredAt)
            .ThenBy(registration =>
                registration.Id)
            .ToListAsync();
    }

    /// <summary>
    /// Lấy các ca thuộc chi nhánh.
    /// </summary>
    public async Task<List<CaShift>>
        GetBranchShiftsAsync(
            int? branchId)
    {
        return await Context.CaShifts
            .AsNoTracking()
            .Where(shift =>
                shift.BranchId == branchId)
            .OrderBy(shift =>
                shift.StartTime)
            .ThenBy(shift =>
                shift.Id)
            .ToListAsync();
    }

    /// <summary>
    /// Lấy cấu hình hoạt động theo ngày của các ca.
    /// </summary>
    public async Task<List<CaBranchShiftConfig>>
        GetShiftConfigsAsync(
            List<int> shiftIds)
    {
        return await Context.CaBranchShiftConfigs
            .AsNoTracking()
            .Where(config =>
                shiftIds.Contains(
                    config.ShiftId))
            .ToListAsync();
    }

    /// <summary>
    /// Lấy lịch cũ thuộc phạm vi đợt đang công bố.
    ///
    /// Có lấy kèm Attendance để tránh xóa lịch
    /// đã phát sinh dữ liệu chấm công.
    /// </summary>
    public async Task<List<CaFinalSchedule>>
        GetExistingSchedulesAsync(
            int periodId,
            DateOnly startDate,
            DateOnly endDate,
            List<int> shiftIds)
    {
        return await _dbSet
            .Include(schedule =>
                schedule.CaAttendances)
            .Where(schedule =>
                schedule.PeriodId == periodId ||
                (
                    schedule.PeriodId == null &&
                    schedule.WorkDate >= startDate &&
                    schedule.WorkDate <= endDate &&
                    shiftIds.Contains(
                        schedule.ShiftId)
                ))
            .ToListAsync();
    }

    /// <summary>
    /// Thêm lịch vào DbContext nhưng chưa lưu.
    /// </summary>
    public void AddSchedule(
        CaFinalSchedule schedule)
    {
        _dbSet.Add(schedule);
    }

    /// <summary>
    /// Đánh dấu xóa lịch nhưng chưa lưu.
    /// </summary>
    public void RemoveSchedule(
        CaFinalSchedule schedule)
    {
        _dbSet.Remove(schedule);
    }

    public async Task SaveChangesAsync()
    {
        await Context.SaveChangesAsync();
    }
}
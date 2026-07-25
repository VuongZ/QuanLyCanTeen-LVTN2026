using LuanVanTotNghiep.backend.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace LuanVanTotNghiep.Repositories;

public class FinalScheduleRepo : Repository<CaFinalSchedule>
{
    public FinalScheduleRepo(AppDbContext context)
        : base(context)
    {
    }

    public override async Task<CaFinalSchedule?> GetbyId(int id)
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

    // Lấy đợt đăng ký để hiển thị lịch.
    public async Task<CaSchedulePeriod?>
        GetPeriodAsNoTrackingAsync(int periodId)
    {
        return await Context.CaSchedulePeriods
            .AsNoTracking()
            .FirstOrDefaultAsync(period =>
                period.Id == periodId);
    }

    // Lấy đợt đăng ký để cập nhật trạng thái.
    public async Task<CaSchedulePeriod?>
        GetPeriodByIdAsync(int periodId)
    {
        return await Context.CaSchedulePeriods
            .FirstOrDefaultAsync(period =>
                period.Id == periodId);
    }

    // Lấy lịch chính thức đã công bố.
    public async Task<List<CaFinalSchedule>>
        GetPublishedSchedulesByPeriodAsync(
            CaSchedulePeriod period)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(schedule => schedule.User)
                .ThenInclude(user => user.Role)
            .Include(schedule => schedule.Shift)
            .Where(schedule =>
                schedule.WorkDate >= period.StartDate &&
                schedule.WorkDate <= period.EndDate &&
                schedule.Shift.BranchId == period.BranchId &&
                schedule.Status == "PUBLISHED")
            .OrderBy(schedule => schedule.WorkDate)
            .ThenBy(schedule => schedule.ShiftId)
            .ThenBy(schedule => schedule.UserId)
            .ToListAsync();
    }

    // Lấy Manager thuộc chi nhánh.
    public async Task<NsUser?> GetBranchManagerAsync(
        int? branchId)
    {
        return await Context.NsUsers
            .Include(user => user.Role)
            .FirstOrDefaultAsync(user =>
                user.BranchId == branchId &&
                user.Role != null &&
                (
                    user.Role.RoleName.Contains("Manager") ||
                    user.Role.RoleName.Contains("MANAGER") ||
                    user.Role.RoleName.Contains("Quản lý") ||
                    user.Role.RoleName.Contains("Quan ly")
                ));
    }

    // Lấy các đăng ký Staff hợp lệ.
    public async Task<List<CaStaffRegistration>>
        GetValidRegistrationsAsync(
            int periodId,
            string[] cancelledStatuses)
    {
        return await Context.CaStaffRegistrations
            .Where(registration =>
                registration.PeriodId == periodId &&
                !cancelledStatuses.Contains(
                    registration.Status))
            .OrderBy(registration =>
                registration.WorkDate)
            .ThenBy(registration =>
                registration.ShiftId)
            .ThenBy(registration =>
                registration.Id)
            .ToListAsync();
    }

    // Lấy các ca thuộc chi nhánh.
    public async Task<List<CaShift>> GetBranchShiftsAsync(
        int? branchId)
    {
        return await Context.CaShifts
            .Where(shift =>
                shift.BranchId == branchId)
            .ToListAsync();
    }

    // Lấy cấu hình hoạt động của các ca.
    public async Task<List<CaBranchShiftConfig>>
        GetShiftConfigsAsync(List<int> shiftIds)
    {
        return await Context.CaBranchShiftConfigs
            .Where(config =>
                shiftIds.Contains(config.ShiftId))
            .ToListAsync();
    }

    // Lấy lịch cũ trong phạm vi của đợt.
    public async Task<List<CaFinalSchedule>>
        GetExistingSchedulesAsync(
            DateOnly startDate,
            DateOnly endDate,
            List<int> shiftIds)
    {
        return await _dbSet
            .Include(schedule =>
                schedule.CaAttendances)
            .Where(schedule =>
                schedule.WorkDate >= startDate &&
                schedule.WorkDate <= endDate &&
                shiftIds.Contains(schedule.ShiftId))
            .ToListAsync();
    }

    // Chỉ thêm vào DbContext, chưa lưu ngay.
    public void AddSchedule(CaFinalSchedule schedule)
    {
        _dbSet.Add(schedule);
    }

    // Chỉ đánh dấu xóa, chưa lưu ngay.
    public void RemoveSchedule(CaFinalSchedule schedule)
    {
        _dbSet.Remove(schedule);
    }

    public async Task SaveChangesAsync()
    {
        await Context.SaveChangesAsync();
    }
}
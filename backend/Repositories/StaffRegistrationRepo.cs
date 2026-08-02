using System.Data;
using LuanVanTotNghiep.backend.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace LuanVanTotNghiep.Repositories;

public class StaffRegistrationRepo
    : Repository<CaStaffRegistration>
{
    public StaffRegistrationRepo(AppDbContext context)
        : base(context)
    {
    }

    public override async Task<CaStaffRegistration?> GetbyId(int id)
    {
        return await _dbSet
            .FirstOrDefaultAsync(registration =>
                registration.Id == id);
    }

    public async Task<IDbContextTransaction>
        BeginSerializableTransactionAsync()
    {
        return await Context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable);
    }

    public async Task<CaSchedulePeriod?> GetPeriodByIdAsync(
        int periodId)
    {
        return await Context.CaSchedulePeriods
            .FirstOrDefaultAsync(period =>
                period.Id == periodId);
    }

    public async Task<NsUser?> GetUserByIdAsync(int userId)
    {
        return await Context.NsUsers
            .FirstOrDefaultAsync(user =>
                user.Id == userId);
    }

    public async Task<CaShift?> GetShiftByIdAsync(int shiftId)
    {
        return await Context.CaShifts
            .FirstOrDefaultAsync(shift =>
                shift.Id == shiftId);
    }

    public async Task<CaBranchShiftConfig?>
        GetShiftConfigAsync(
            int shiftId,
            string dayOfWeek)
    {
        return await Context.CaBranchShiftConfigs
            .FirstOrDefaultAsync(config =>
                config.ShiftId == shiftId &&
                config.DayOfWeek == dayOfWeek);
    }

    /// <summary>
    /// Kiểm tra nhân viên đã có phiếu đăng ký chưa bị hủy
    /// trong đúng đợt, ngày và ca hay chưa.
    /// </summary>
    public async Task<bool>
        HasNonCancelledRegistrationAsync(
            int periodId,
            int userId,
            int shiftId,
            DateOnly workDate)
    {
        return await _dbSet.AnyAsync(registration =>
            registration.PeriodId == periodId &&
            registration.UserId == userId &&
            registration.ShiftId == shiftId &&
            registration.WorkDate == workDate &&
            registration.Status != "CANCELLED");
    }

    /// <summary>
    /// Chỉ đếm người giữ vị trí chính thức.
    /// WAITLIST không chiếm số lượng của ca.
    /// </summary>
    public async Task<int>
        CountRegisteredAsync(
            int periodId,
            int shiftId,
            DateOnly workDate)
    {
        return await _dbSet.CountAsync(registration =>
            registration.PeriodId == periodId &&
            registration.ShiftId == shiftId &&
            registration.WorkDate == workDate &&
            registration.Status == "REGISTERED");
    }

    /// <summary>
    /// Lấy người đăng ký chờ sớm nhất.
    /// ID được dùng để phân xử khi hai dòng có cùng thời gian.
    /// </summary>
    public async Task<CaStaffRegistration?>
        GetOldestWaitlistAsync(
            int periodId,
            int shiftId,
            DateOnly workDate)
    {
        return await _dbSet
            .Where(registration =>
                registration.PeriodId == periodId &&
                registration.ShiftId == shiftId &&
                registration.WorkDate == workDate &&
                registration.Status == "WAITLIST")
            .OrderBy(registration =>
                registration.RegisteredAt)
            .ThenBy(registration =>
                registration.Id)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<CaStaffRegistration>>
        GetMyRegistrationsAsync(
            int userId,
            int periodId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(registration =>
                registration.Shift)
            .Where(registration =>
                registration.UserId == userId &&
                registration.PeriodId == periodId)
            .OrderBy(registration =>
                registration.WorkDate)
            .ThenBy(registration =>
                registration.ShiftId)
            .ThenBy(registration =>
                registration.RegisteredAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<CaStaffRegistration>>
        GetRegistrationsByPeriodAsync(int periodId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(registration =>
                registration.User)
            .Include(registration =>
                registration.Shift)
            .Where(registration =>
                registration.PeriodId == periodId)
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
}
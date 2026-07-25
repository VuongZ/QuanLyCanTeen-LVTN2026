using LuanVanTotNghiep.backend.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

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

    public async Task<bool> HasActiveRegistrationAsync(
        int periodId,
        int userId,
        int shiftId,
        DateOnly workDate,
        string[] cancelledStatuses)
    {
        return await _dbSet.AnyAsync(registration =>
            registration.PeriodId == periodId &&
            registration.UserId == userId &&
            registration.ShiftId == shiftId &&
            registration.WorkDate == workDate &&
            !cancelledStatuses.Contains(
                registration.Status));
    }

    public async Task<int> CountActiveRegistrationsAsync(
        int periodId,
        int shiftId,
        DateOnly workDate,
        string[] cancelledStatuses)
    {
        return await _dbSet.CountAsync(registration =>
            registration.PeriodId == periodId &&
            registration.ShiftId == shiftId &&
            registration.WorkDate == workDate &&
            !cancelledStatuses.Contains(
                registration.Status));
    }

    public async Task<IEnumerable<CaStaffRegistration>>
        GetMyRegistrationsAsync(
            int userId,
            int periodId)
    {
        return await _dbSet
            .Include(registration => registration.Shift)
            .Where(registration =>
                registration.UserId == userId &&
                registration.PeriodId == periodId)
            .OrderBy(registration =>
                registration.WorkDate)
            .ThenBy(registration =>
                registration.ShiftId)
            .ToListAsync();
    }

    public async Task<IEnumerable<CaStaffRegistration>>
        GetRegistrationsByPeriodAsync(int periodId)
    {
        return await _dbSet
            .Include(registration =>
                registration.User)
            .Include(registration =>
                registration.Shift)
            .Where(registration =>
                registration.PeriodId == periodId)
            .OrderBy(registration =>
                registration.WorkDate)
            .ThenBy(registration =>
                registration.Id)
            .ToListAsync();
    }
}
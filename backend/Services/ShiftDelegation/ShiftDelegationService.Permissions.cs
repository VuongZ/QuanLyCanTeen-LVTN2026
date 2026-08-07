using System.Globalization;
using System.Text;
using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Services;

public partial class ShiftDelegationService
{
public async Task<bool> HasActivePermissionAsync(
        int actorId,
        int branchId,
        int shiftId,
        DateOnly workDate)
    {
        var branchIsActive = await context.DmBranches
            .AsNoTracking()
            .AnyAsync(branch =>
                branch.Id == branchId &&
                branch.IsActive);

        var shiftIsActive = await context.CaShifts
            .AsNoTracking()
            .AnyAsync(shift =>
                shift.Id == shiftId &&
                shift.IsActive);

        if (!branchIsActive || !shiftIsActive)
        {
            return false;
        }

        var now = UtcNow();
        return await context.CaShiftDelegations.AnyAsync(item =>
            item.DelegateUserId == actorId &&
            item.BranchId == branchId &&
            item.ShiftId == shiftId &&
            item.WorkDate == workDate &&
            item.Status == Accepted &&
            item.StartsAtUtc <= now &&
            item.EndsAtUtc >= now);
    }

    public async Task LogActiveActionAsync(
        int actorId,
        int branchId,
        int shiftId,
        DateOnly workDate,
        string actionType,
        string? details)
    {
        var now = UtcNow();
        var delegation = await context.CaShiftDelegations
            .FirstOrDefaultAsync(item =>
                item.DelegateUserId == actorId &&
                item.BranchId == branchId &&
                item.ShiftId == shiftId &&
                item.WorkDate == workDate &&
                item.Status == Accepted &&
                item.StartsAtUtc <= now &&
                item.EndsAtUtc >= now);
        if (delegation != null)
            await AddAuditAsync(delegation.Id, actorId, actionType, details);
    }
}

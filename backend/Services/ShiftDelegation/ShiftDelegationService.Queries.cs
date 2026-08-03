using System.Globalization;
using System.Text;
using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Services;

public partial class ShiftDelegationService
{
public async Task<List<ShiftDelegationDto>> GetVisibleAsync(
        int actorId,
        int? requestedBranchId)
    {
        await ExpireEndedDelegationsAsync();
        var actor = await GetActorAsync(actorId);
        var role = Normalize(actor.Role?.RoleName);

        var query = IncludedQuery().AsNoTracking();
        if (IsAdmin(role))
        {
            if (requestedBranchId is > 0)
                query = query.Where(item => item.BranchId == requestedBranchId);
        }
        else if (IsManager(role))
        {
            if (actor.BranchId is not > 0)
                throw new InvalidOperationException("Quản lý chưa được gán chi nhánh.");

            query = query.Where(item => item.BranchId == actor.BranchId);
        }
        else
        {
            query = query.Where(item =>
                item.DelegateUserId == actorId ||
                item.DelegatedByUserId == actorId);
        }

        var items = await query
            .OrderByDescending(item => item.WorkDate)
            .ThenByDescending(item => item.RequestedAtUtc)
            .Take(200)
            .ToListAsync();

        return items.Select(ToDto).ToList();
    }
}


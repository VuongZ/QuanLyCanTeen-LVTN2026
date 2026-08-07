using System.Globalization;
using System.Text;
using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Services;

public partial class ShiftDelegationService
{
public async Task<ShiftDelegationDto> RespondAsync(
        int actorId,
        int delegationId,
        bool accept)
    {
        await ExpireEndedDelegationsAsync();
        await using var transaction =
            await context.Database.BeginTransactionAsync();
        await context.Database
            .SqlQueryRaw<int>(
                "SELECT id AS Value FROM ca_shift_delegation WHERE id = {0} FOR UPDATE",
                delegationId)
            .SingleOrDefaultAsync();

        var delegation = await context.CaShiftDelegations
            .FirstOrDefaultAsync(item => item.Id == delegationId);
        if (delegation == null)
            throw new KeyNotFoundException("Không tìm thấy yêu cầu ủy quyền.");
        if (delegation.DelegateUserId != actorId)
            throw new InvalidOperationException(
                "Chỉ người được ủy quyền mới có thể xác nhận yêu cầu.");
        if (delegation.Status != Pending)
            throw new InvalidOperationException("Yêu cầu này đã được xử lý.");
        if (delegation.EndsAtUtc <= DateTime.UtcNow)
            throw new InvalidOperationException("Ca làm đã kết thúc.");

        if (accept)
        {
            var branchIsActive = await context.DmBranches
                .AsNoTracking()
                .AnyAsync(branch =>
                    branch.Id == delegation.BranchId &&
                    branch.IsActive);

            var shiftIsActive = await context.CaShifts
                .AsNoTracking()
                .AnyAsync(shift =>
                    shift.Id == delegation.ShiftId &&
                    shift.IsActive);

            if (!branchIsActive || !shiftIsActive)
            {
                throw new InvalidOperationException(
                    "Cơ sở hoặc ca làm đã ngừng hoạt động nên không thể nhận ủy quyền.");
            }
        }

        delegation.Status = accept ? Accepted : Rejected;
        delegation.RespondedAtUtc = UtcNow();
        await context.SaveChangesAsync();
        await AddAuditAsync(
            delegation.Id,
            actorId,
            accept ? "DELEGATION_ACCEPTED" : "DELEGATION_REJECTED",
            accept ? "Nhân viên đã nhận quyền trưởng ca tạm thời." : "Nhân viên đã từ chối ủy quyền.");
        await transaction.CommitAsync();

        return ToDto(await IncludedQuery()
            .AsNoTracking()
            .FirstAsync(item => item.Id == delegation.Id));
    }

    public async Task<ShiftDelegationDto> RevokeAsync(
        int actorId,
        int delegationId)
    {
        var actor = await GetActorAsync(actorId);
        var role = Normalize(actor.Role?.RoleName);
        await using var transaction =
            await context.Database.BeginTransactionAsync();
        await context.Database
            .SqlQueryRaw<int>(
                "SELECT id AS Value FROM ca_shift_delegation WHERE id = {0} FOR UPDATE",
                delegationId)
            .SingleOrDefaultAsync();

        var delegation = await context.CaShiftDelegations
            .FirstOrDefaultAsync(item => item.Id == delegationId);
        if (delegation == null)
            throw new KeyNotFoundException("Không tìm thấy yêu cầu ủy quyền.");

        var allowed = IsAdmin(role) ||
            (IsManager(role) && actor.BranchId == delegation.BranchId) ||
            delegation.DelegatedByUserId == actorId;
        if (!allowed)
            throw new InvalidOperationException("Bạn không có quyền thu hồi ủy quyền này.");
        if (delegation.Status != Pending && delegation.Status != Accepted)
            throw new InvalidOperationException("Ủy quyền này không còn hiệu lực để thu hồi.");

        delegation.Status = Revoked;
        delegation.RevokedAtUtc = UtcNow();
        await context.SaveChangesAsync();
        await AddAuditAsync(
            delegation.Id,
            actorId,
            "DELEGATION_REVOKED",
            "Quyền trưởng ca tạm thời đã được thu hồi.");
        await transaction.CommitAsync();

        return ToDto(await IncludedQuery()
            .AsNoTracking()
            .FirstAsync(item => item.Id == delegation.Id));
    }
}


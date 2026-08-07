using System.Globalization;
using System.Text;
using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Services;

public partial class ShiftDelegationService
{
public async Task<ShiftDelegationDto> CreateAsync(
        int actorId,
        CreateShiftDelegationDto dto)
    {
        await ExpireEndedDelegationsAsync();
        var actor = await GetActorAsync(actorId);
        var actorRole = Normalize(actor.Role?.RoleName);
        if (!IsAdmin(actorRole) && !IsManager(actorRole))
            throw new InvalidOperationException(
                "Chỉ Quản lý chi nhánh hoặc Admin mới được tạo ủy quyền.");

        var branchId = IsAdmin(actorRole)
            ? dto.BranchId
            : actor.BranchId;

        if (branchId is not > 0)
            throw new InvalidOperationException("Vui lòng chọn chi nhánh cần ủy quyền.");

        var branchIsActive = await context.DmBranches
            .AsNoTracking()
            .AnyAsync(branch =>
                branch.Id == branchId.Value &&
                branch.IsActive);

        if (!branchIsActive)
            throw new InvalidOperationException(
                "Cơ sở đã ngừng hoạt động nên không thể tạo ủy quyền ca mới.");

        var shift = await context.CaShifts
            .FirstOrDefaultAsync(item =>
                item.Id == dto.ShiftId &&
                item.BranchId == branchId);
        if (shift == null)
            throw new InvalidOperationException("Ca làm không thuộc chi nhánh đã chọn.");

        if (!shift.IsActive)
            throw new InvalidOperationException(
                "Ca làm đã ngừng hoạt động nên không thể tạo ủy quyền mới.");

        var delegateUser = await context.NsUsers
            .Include(user => user.Role)
            .FirstOrDefaultAsync(user =>
                user.Id == dto.DelegateUserId &&
                user.IsDeleted != true);
        if (delegateUser == null || delegateUser.BranchId != branchId)
            throw new InvalidOperationException(
                "Người được ủy quyền phải là nhân viên trong cùng chi nhánh.");

        if (!IsStaff(Normalize(delegateUser.Role?.RoleName)))
            throw new InvalidOperationException(
                "Chỉ có thể ủy quyền cho tài khoản nhân viên.");

        if (delegateUser.Id == actorId)
            throw new InvalidOperationException("Không thể tự ủy quyền cho chính mình.");

        var hasPublishedSchedule = await context.CaFinalSchedules.AnyAsync(schedule =>
            schedule.UserId == delegateUser.Id &&
            schedule.ShiftId == shift.Id &&
            schedule.WorkDate == dto.WorkDate &&
            schedule.Status == "PUBLISHED");
        if (!hasPublishedSchedule)
            throw new InvalidOperationException(
                "Người được chọn chưa có lịch chính thức trong ca này.");

        var (startsAtUtc, endsAtUtc) =
            BuildUtcRange(dto.WorkDate, shift.StartTime, shift.EndTime);
        if (endsAtUtc <= DateTime.UtcNow)
            throw new InvalidOperationException("Không thể ủy quyền cho ca đã kết thúc.");

        await using var transaction =
            await context.Database.BeginTransactionAsync();
        await context.Database
            .SqlQueryRaw<int>(
                "SELECT id AS Value FROM ca_shift WHERE id = {0} FOR UPDATE",
                shift.Id)
            .SingleAsync();

        var hasOpenDelegation = await context.CaShiftDelegations.AnyAsync(item =>
            item.BranchId == branchId &&
            item.ShiftId == shift.Id &&
            item.WorkDate == dto.WorkDate &&
            (item.Status == Pending || item.Status == Accepted));
        if (hasOpenDelegation)
            throw new InvalidOperationException(
                "Ca này đã có một yêu cầu ủy quyền đang chờ hoặc đã được chấp nhận.");

        var delegateHasOverlap = await context.CaShiftDelegations.AnyAsync(item =>
            item.DelegateUserId == delegateUser.Id &&
            (item.Status == Pending || item.Status == Accepted) &&
            item.StartsAtUtc < endsAtUtc &&
            item.EndsAtUtc > startsAtUtc);
        if (delegateHasOverlap)
            throw new InvalidOperationException(
                "Nhân viên này đã có một ủy quyền khác trùng thời gian.");

        var reason = dto.Reason?.Trim();
        if (string.IsNullOrWhiteSpace(reason) || reason.Length < 3)
            throw new InvalidOperationException("Vui lòng nhập lý do ủy quyền.");

        var now = UtcNow();
        var delegation = new CaShiftDelegation
        {
            BranchId = branchId.Value,
            ShiftId = shift.Id,
            WorkDate = dto.WorkDate,
            DelegatedByUserId = actorId,
            DelegateUserId = delegateUser.Id,
            Reason = reason,
            Status = Pending,
            StartsAtUtc = startsAtUtc,
            EndsAtUtc = endsAtUtc,
            RequestedAtUtc = now
        };
        context.CaShiftDelegations.Add(delegation);
        await context.SaveChangesAsync();
        await AddAuditAsync(
            delegation.Id,
            actorId,
            "DELEGATION_CREATED",
            $"Ủy quyền ca {shift.ShiftName} ngày {dto.WorkDate:dd/MM/yyyy} cho {delegateUser.FullName}.");
        await transaction.CommitAsync();

        return ToDto((await IncludedQuery()
            .AsNoTracking()
            .FirstAsync(item => item.Id == delegation.Id)));
    }
}

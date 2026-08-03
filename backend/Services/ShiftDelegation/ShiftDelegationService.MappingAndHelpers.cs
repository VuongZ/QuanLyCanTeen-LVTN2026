using System.Globalization;
using System.Text;
using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Services;

public partial class ShiftDelegationService
{
private IQueryable<CaShiftDelegation> IncludedQuery() =>
        context.CaShiftDelegations
            .Include(item => item.Branch)
            .Include(item => item.Shift)
            .Include(item => item.DelegatedByUser)
            .Include(item => item.DelegateUser)
            .Include(item => item.Audits)
                .ThenInclude(audit => audit.ActorUser);

    private async Task<NsUser> GetActorAsync(int actorId) =>
        await context.NsUsers
            .Include(user => user.Role)
            .FirstOrDefaultAsync(user => user.Id == actorId && user.IsDeleted != true)
        ?? throw new KeyNotFoundException("Không tìm thấy tài khoản.");

    private async Task ExpireEndedDelegationsAsync()
    {
        var now = UtcNow();
        var ended = await context.CaShiftDelegations
            .Where(item =>
                (item.Status == Pending || item.Status == Accepted) &&
                item.EndsAtUtc < now)
            .ToListAsync();
        if (ended.Count == 0)
            return;

        foreach (var item in ended)
            item.Status = Expired;
        await context.SaveChangesAsync();
    }

    private async Task AddAuditAsync(
        int delegationId,
        int actorId,
        string actionType,
        string? details)
    {
        context.CaShiftDelegationAudits.Add(new CaShiftDelegationAudit
        {
            DelegationId = delegationId,
            ActorUserId = actorId,
            ActionType = actionType,
            Details = details,
            OccurredAtUtc = UtcNow()
        });
        await context.SaveChangesAsync();
    }

    private static ShiftDelegationDto ToDto(CaShiftDelegation item)
    {
        var now = DateTime.UtcNow;
        var status =
            (item.Status == Accepted || item.Status == Pending) &&
            item.EndsAtUtc < now
            ? Expired
            : item.Status;
        return new ShiftDelegationDto
        {
            Id = item.Id,
            BranchId = item.BranchId,
            BranchName = item.Branch?.Name,
            ShiftId = item.ShiftId,
            ShiftName = item.Shift?.ShiftName,
            WorkDate = item.WorkDate,
            DelegatedByUserId = item.DelegatedByUserId,
            DelegatedByName = item.DelegatedByUser?.FullName,
            DelegateUserId = item.DelegateUserId,
            DelegateUserName = item.DelegateUser?.FullName,
            Reason = item.Reason,
            Status = status,
            StartsAtUtc = item.StartsAtUtc,
            EndsAtUtc = item.EndsAtUtc,
            RequestedAtUtc = item.RequestedAtUtc,
            RespondedAtUtc = item.RespondedAtUtc,
            RevokedAtUtc = item.RevokedAtUtc,
            IsPermissionActive = item.Status == Accepted &&
                item.StartsAtUtc <= now &&
                item.EndsAtUtc >= now,
            Audits = item.Audits
                .OrderByDescending(audit => audit.OccurredAtUtc)
                .Select(audit => new ShiftDelegationAuditDto
                {
                    Id = audit.Id,
                    ActorUserId = audit.ActorUserId,
                    ActorName = audit.ActorUser?.FullName,
                    ActionType = audit.ActionType,
                    Details = audit.Details,
                    OccurredAtUtc = audit.OccurredAtUtc
                })
                .ToList()
        };
    }

    private static (DateTime StartUtc, DateTime EndUtc) BuildUtcRange(
        DateOnly workDate,
        TimeOnly startTime,
        TimeOnly endTime)
    {
        var startLocal = workDate.ToDateTime(startTime, DateTimeKind.Unspecified);
        var endDate = endTime <= startTime ? workDate.AddDays(1) : workDate;
        var endLocal = endDate.ToDateTime(endTime, DateTimeKind.Unspecified);
        var zone = GetVietnamTimeZone();
        return (
            DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeToUtc(startLocal, zone), DateTimeKind.Unspecified),
            DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeToUtc(endLocal, zone), DateTimeKind.Unspecified));
    }

    private static TimeZoneInfo GetVietnamTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        }
    }

    private static DateTime UtcNow() =>
        DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        var builder = new StringBuilder();
        foreach (var character in value.Normalize(NormalizationForm.FormD))
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(character);
        return builder.ToString().Normalize(NormalizationForm.FormC).Trim().ToUpperInvariant();
    }

    private static bool IsAdmin(string role) =>
        role.Contains("ADMIN") || role.Contains("QUAN TRI");
    private static bool IsManager(string role) =>
        role.Contains("MANAGER") || role.Contains("QUAN LY");
    private static bool IsStaff(string role) =>
        role.Contains("STAFF") || role.Contains("NHAN VIEN");
}


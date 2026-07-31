using System.Globalization;
using System.Text;
using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Services;

public class ShiftDelegationService(AppDbContext context)
{
    public const string Pending = "PENDING";
    public const string Accepted = "ACCEPTED";
    public const string Rejected = "REJECTED";
    public const string Revoked = "REVOKED";
    public const string Expired = "EXPIRED";

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

        var shift = await context.CaShifts
            .FirstOrDefaultAsync(item =>
                item.Id == dto.ShiftId &&
                item.BranchId == branchId);
        if (shift == null)
            throw new InvalidOperationException("Ca làm không thuộc chi nhánh đã chọn.");

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

    public async Task<bool> HasActivePermissionAsync(
        int actorId,
        int branchId,
        int shiftId,
        DateOnly workDate)
    {
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

    public async Task<object> MarkAttendanceStatusAsync(
        int actorId,
        MarkDelegatedAttendanceDto dto)
    {
        var actor = await GetActorAsync(actorId);
        if (actor.BranchId is not int branchId)
            throw new InvalidOperationException("Tài khoản chưa được gán chi nhánh.");

        var actorRole = Normalize(actor.Role?.RoleName);
        var isManager = IsManager(actorRole);
        var hasDelegation = await HasActivePermissionAsync(
            actorId, branchId, dto.ShiftId, dto.WorkDate);
        if (!isManager && !hasDelegation)
            throw new InvalidOperationException(
                "Bạn không có quyền trưởng ca trong thời gian của ca này.");

        var status = Normalize(dto.Status);
        if (status != "LATE" && status != "ABSENT")
            throw new InvalidOperationException("Trạng thái phải là LATE hoặc ABSENT.");

        var noteValue = dto.Note?.Trim();
        if (noteValue?.Length > 500)
            throw new InvalidOperationException("Ghi chú không được vượt quá 500 ký tự.");

        var schedule = await context.CaFinalSchedules
            .Include(item => item.User)
            .Include(item => item.Shift)
            .FirstOrDefaultAsync(item =>
                item.UserId == dto.EmployeeId &&
                item.ShiftId == dto.ShiftId &&
                item.WorkDate == dto.WorkDate &&
                item.Status == "PUBLISHED");
        if (schedule == null || schedule.User.BranchId != branchId)
            throw new InvalidOperationException(
                "Nhân viên không có lịch chính thức trong ca này.");

        var attendance = await context.CaAttendances
            .FirstOrDefaultAsync(item => item.ScheduleId == schedule.Id);
        if (attendance == null)
        {
            attendance = new CaAttendance { ScheduleId = schedule.Id };
            context.CaAttendances.Add(attendance);
        }

        if (status == "ABSENT" && attendance.CheckInTime != null)
            throw new InvalidOperationException(
                "Nhân viên đã check-in nên không thể ghi nhận vắng mặt.");

        attendance.Status = status == "LATE"
            ? "Đi muộn"
            : "Vắng mặt";
        await context.SaveChangesAsync();

        var note = string.IsNullOrWhiteSpace(noteValue) ? "" : $" Lý do: {noteValue}";
        await LogActiveActionAsync(
            actorId,
            branchId,
            dto.ShiftId,
            dto.WorkDate,
            status == "LATE" ? "ATTENDANCE_MARKED_LATE" : "ATTENDANCE_MARKED_ABSENT",
            $"Ghi nhận {schedule.User.FullName}: {attendance.Status}.{note}");

        return new
        {
            attendance.Id,
            scheduleId = schedule.Id,
            employeeId = schedule.UserId,
            employeeName = schedule.User.FullName,
            attendance.Status
        };
    }

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

using System.ComponentModel.DataAnnotations;

namespace LuanVanTotNghiep.DTOs;

public class CreateShiftDelegationDto
{
    public int? BranchId { get; set; }
    [Required] public int ShiftId { get; set; }
    [Required] public DateOnly WorkDate { get; set; }
    [Required] public int DelegateUserId { get; set; }
    [Required, StringLength(500, MinimumLength = 3)]
    public string Reason { get; set; } = null!;
}

public class RespondShiftDelegationDto
{
    [Required] public bool Accept { get; set; }
}

public class MarkDelegatedAttendanceDto
{
    [Required] public int EmployeeId { get; set; }
    [Required] public int ShiftId { get; set; }
    [Required] public DateOnly WorkDate { get; set; }
    [Required] public string Status { get; set; } = null!;
    public string? Note { get; set; }
}

public class ShiftDelegationAuditDto
{
    public int Id { get; set; }
    public int ActorUserId { get; set; }
    public string? ActorName { get; set; }
    public string ActionType { get; set; } = null!;
    public string? Details { get; set; }
    public DateTime OccurredAtUtc { get; set; }
}

public class ShiftDelegationDto
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public string? BranchName { get; set; }
    public int ShiftId { get; set; }
    public string? ShiftName { get; set; }
    public DateOnly WorkDate { get; set; }
    public int DelegatedByUserId { get; set; }
    public string? DelegatedByName { get; set; }
    public int DelegateUserId { get; set; }
    public string? DelegateUserName { get; set; }
    public string Reason { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public DateTime RequestedAtUtc { get; set; }
    public DateTime? RespondedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public bool IsPermissionActive { get; set; }
    public List<ShiftDelegationAuditDto> Audits { get; set; } = [];
}

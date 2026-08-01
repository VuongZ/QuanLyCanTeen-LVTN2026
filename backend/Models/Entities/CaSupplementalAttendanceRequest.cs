namespace LuanVanTotNghiep.backend.Models.Entities;

public class CaSupplementalAttendanceRequest
{
    public int Id { get; set; }
    public int ScheduleId { get; set; }
    public int RequestedByManagerId { get; set; }
    public DateTime ProposedCheckInTime { get; set; }
    public DateTime ProposedCheckOutTime { get; set; }
    public string? Reason { get; set; }
    public string Status { get; set; } = "PENDING";
    public int? ReviewedByAdminId { get; set; }
    public string? RejectReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }

    public virtual CaFinalSchedule Schedule { get; set; } = null!;
    public virtual NsUser RequestedByManager { get; set; } = null!;
    public virtual NsUser? ReviewedByAdmin { get; set; }
}

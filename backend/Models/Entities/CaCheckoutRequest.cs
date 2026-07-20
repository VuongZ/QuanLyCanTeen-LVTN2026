namespace LuanVanTotNghiep.backend.Models.Entities;

public class CaCheckoutRequest
{
    public int Id { get; set; }
    public int AttendanceId { get; set; }
    public int RequestedByUserId { get; set; }
    public DateTime ProposedCheckOutTime { get; set; }
    public DateTime? RequestedCheckOutTime { get; set; }
    public string? Reason { get; set; }
    public string Status { get; set; } = "AWAITING_EMPLOYEE";
    public int? ReviewedByUserId { get; set; }
    public string? RejectReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }

    public virtual CaAttendance Attendance { get; set; } = null!;
    public virtual NsUser RequestedByUser { get; set; } = null!;
    public virtual NsUser? ReviewedByUser { get; set; }
    public virtual ICollection<CaCheckoutRequestHistory> History { get; set; } = new List<CaCheckoutRequestHistory>();
}

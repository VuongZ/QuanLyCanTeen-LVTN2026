namespace LuanVanTotNghiep.DTOs;

public class SubmitCheckoutRequestDto
{
    public DateTime? CheckOutTime { get; set; }
    public string? Reason { get; set; }
}

public class RejectCheckoutRequestDto
{
    public string? Reason { get; set; }
}

public class CheckoutRequestDto
{
    public int Id { get; set; }
    public int AttendanceId { get; set; }
    public int ScheduleId { get; set; }
    public int UserId { get; set; }
    public string? FullName { get; set; }
    public string? RoleName { get; set; }
    public string? BranchName { get; set; }
    public string? ShiftName { get; set; }
    public string WorkDate { get; set; } = "";
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime ProposedCheckOutTime { get; set; }
    public DateTime? RequestedCheckOutTime { get; set; }
    public string? Reason { get; set; }
    public string Status { get; set; } = "";
    public string? RejectReason { get; set; }
    public string? ReviewerName { get; set; }
    public DateTime UpdatedAt { get; set; }
}

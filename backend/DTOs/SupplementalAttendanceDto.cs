namespace LuanVanTotNghiep.DTOs;

public class SubmitSupplementalAttendanceDto
{
    public List<SupplementalAttendanceEntryDto> Entries { get; set; } = [];
    public string? Reason { get; set; }
}

public class SupplementalAttendanceEntryDto
{
    public int ScheduleId { get; set; }
    public DateTime CheckInTime { get; set; }
}

public class RejectSupplementalAttendanceDto
{
    public string? Reason { get; set; }
}

public class SupplementalAttendanceCandidateDto
{
    public int ScheduleId { get; set; }
    public int EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public int ShiftId { get; set; }
    public string ShiftName { get; set; } = "";
    public string StartTime { get; set; } = "";
    public string EndTime { get; set; } = "";
    public int? PreviousRequestId { get; set; }
    public string? PreviousCheckInTime { get; set; }
    public string? PreviousRejectReason { get; set; }
}

public class SupplementalAttendanceRequestDto
{
    public int Id { get; set; }
    public int ScheduleId { get; set; }
    public int EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public string? BranchName { get; set; }
    public string ShiftName { get; set; } = "";
    public string WorkDate { get; set; } = "";
    public string StartTime { get; set; } = "";
    public string EndTime { get; set; } = "";
    public DateTime ProposedCheckInTime { get; set; }
    public DateTime ProposedCheckOutTime { get; set; }
    public decimal WorkedHours { get; set; }
    public string? Reason { get; set; }
    public string Status { get; set; } = "";
    public string? ManagerName { get; set; }
    public string? AdminName { get; set; }
    public string? RejectReason { get; set; }
    public DateTime UpdatedAt { get; set; }
}

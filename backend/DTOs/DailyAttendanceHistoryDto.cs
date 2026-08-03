namespace LuanVanTotNghiep.DTOs;

public class DailyAttendanceHistoryDto
{
    public int AttendanceId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string? RoleName { get; set; }
    public int ShiftId { get; set; }
    public string ShiftName { get; set; } = string.Empty;
    public DateOnly WorkDate { get; set; }
    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public decimal WorkedHours { get; set; }
    public string Status { get; set; } = string.Empty;
}

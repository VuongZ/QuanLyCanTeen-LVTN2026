namespace LuanVanTotNghiep.DTOs;

public class SalaryWorkDetailDto
{
    public int AttendanceId { get; set; }
    public int ScheduleId { get; set; }

    public DateOnly WorkDate { get; set; }

    public int ShiftId { get; set; }
    public string ShiftName { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;

    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }

    public decimal WorkedHours { get; set; }
    public string Status { get; set; } = string.Empty;
}
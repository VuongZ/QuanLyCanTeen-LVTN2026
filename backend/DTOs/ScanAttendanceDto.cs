using System;
using System.ComponentModel.DataAnnotations;

namespace LuanVanTotNghiep.DTOs;

public class ScanAttendanceDto
{
    [Required]
    public int ManagerId { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public int ShiftId { get; set; }

    [Required]
    public DateOnly WorkDate { get; set; }

    public string? Action { get; set; }

    public DateTime? CheckInTime { get; set; }

    public DateTime? CheckOutTime { get; set; }
}

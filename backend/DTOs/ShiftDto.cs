namespace LuanVanTotNghiep.DTOs;

public class ShiftDto
{
    public int Id { get; set; }
    public string ShiftName { get; set; } = null!;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int? MaxStaff { get; set; }
    public bool? IsOt { get; set; }
    public string? BranchName { get; set; }
    public int? BranchId { get; set; }
    public bool IsActive { get; set; }
    public DateTime? InactiveAt { get; set; }
    public int? InactiveBy { get; set; }
    public string? InactiveReason { get; set; }
}

public class ChangeShiftStatusDto
{
    public string? Reason { get; set; }
}
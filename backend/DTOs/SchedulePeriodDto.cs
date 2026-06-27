namespace LuanVanTotNghiep.DTOs;

public class SchedulePeriodDto
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public string? BranchName { get; set; } 
   
    public DateOnly StartDate { get; set; } 
    public DateOnly EndDate { get; set; } 
    public string? Status { get; set; }
}
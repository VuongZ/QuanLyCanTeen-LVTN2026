namespace LuanVanTotNghiep.DTOs;

public class WorkHoursRankingDto
{
    public int UserId { get; set; }
    public string EmployeeName { get; set; } = "";
    public int? BranchId { get; set; }
    public string? BranchName { get; set; }
    public decimal TotalHours { get; set; }
    public int ShiftCount { get; set; }
    public int Rank { get; set; }
}

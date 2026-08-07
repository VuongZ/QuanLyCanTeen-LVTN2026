namespace LuanVanTotNghiep.DTOs;

public class BranchDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Address { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool IsActive { get; set; }
    public DateTime? InactiveAt { get; set; }
    public int? InactiveBy { get; set; }
    public string? InactiveReason { get; set; }
}

public class ChangeBranchStatusDto
{
    public string? Reason { get; set; }
}
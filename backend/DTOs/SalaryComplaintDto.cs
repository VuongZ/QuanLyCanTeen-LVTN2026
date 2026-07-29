namespace LuanVanTotNghiep.DTOs;

public class SalaryComplaintDto
{
    public int Id { get; set; }
    public int SalaryId { get; set; }
    public int UserId { get; set; }
    public string? EmployeeName { get; set; }
    public string? BranchName { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Status { get; set; } = "PENDING";
    public string? ManagerResponse { get; set; }
    public int? ReviewedByUserId { get; set; }
    public string? ReviewedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
}

public class CreateSalaryComplaintDto
{
    public string Content { get; set; } = string.Empty;
}

public class ResolveSalaryComplaintDto
{
    public string Response { get; set; } = string.Empty;
}

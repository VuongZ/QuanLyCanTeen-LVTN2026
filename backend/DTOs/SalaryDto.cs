namespace LuanVanTotNghiep.DTOs;

public class SalaryDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? Username { get; set; }
    public string? FullName { get; set; }
    public string? BranchName { get; set; }
    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankAccountName { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal TotalHours { get; set; }
    public decimal HourlyWageAtTime { get; set; }
    public decimal TotalSalary { get; set; }
    public decimal TotalBonus { get; set; }
    public decimal TotalPenalty { get; set; }
    public string? Status { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? CreatedAt { get; set; }
}

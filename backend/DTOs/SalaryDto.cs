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
    public DateTime? FinalizedAt { get; set; }
    public int? FinalizedByUserId { get; set; }
    public string? FinalizedByName { get; set; }
    public DateTime? AdminFinalizedAt { get; set; }
    public int? AdminFinalizedByUserId { get; set; }
    public string? AdminFinalizedByName { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public class BranchSalarySummaryDto
{
    public int? BranchId { get; set; }
    public string? BranchName { get; set; }
    public int? ManagerId { get; set; }
    public string? ManagerName { get; set; }
    public string? ManagerEmail { get; set; }
    public string? ManagerPhoneNumber { get; set; }
    public string? ManagerBankName { get; set; }
    public string? ManagerBankAccountNumber { get; set; }
    public string? ManagerBankAccountName { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public int SalaryCount { get; set; }
    public decimal PendingTotal { get; set; }
    public decimal PaidTotal { get; set; }
    public decimal TotalSalary { get; set; }
    public int PendingCount { get; set; }
    public int PaidCount { get; set; }
    public int EmployeeCount { get; set; }
    public int? TransferId { get; set; }
    public bool IsTransferred { get; set; }
    public decimal TransferredAmount { get; set; }
    public DateTime? TransferredAt { get; set; }
    public string? TransferredByName { get; set; }
}

namespace LuanVanTotNghiep.DTOs;

public class SalaryRuleAdjustmentDto
{
    public int UserId { get; set; }
    public int? SalaryId { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? RoleName { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public int WorkedDays { get; set; }
    public int LateCount { get; set; }
    public int AbsentCount { get; set; }
    public decimal CurrentBonus { get; set; }
    public decimal CurrentPenalty { get; set; }
    public decimal CalculatedBonus { get; set; }
    public decimal CalculatedPenalty { get; set; }
    public decimal TotalHours { get; set; }
    public decimal HourlyWageAtTime { get; set; }
    public decimal TotalSalary { get; set; }
    public string? Status { get; set; }
}

public class SalaryRuleDto
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public int BonusThresholdDays { get; set; }
    public decimal BonusAmount { get; set; }
    public decimal LatePenalty { get; set; }
    public decimal AbsentPenalty { get; set; }
    public float WeekendMultiplier { get; set; }
}

public class SalaryRuleAdjustmentPageDto
{
    public SalaryRuleDto? Rule { get; set; }
    public IEnumerable<SalaryRuleAdjustmentDto> Employees { get; set; } = [];
}

public class ApplySalaryRuleDto
{
    public int UserId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
}

public class ManualSalaryAdjustmentDto
{
    public int UserId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal BonusAmount { get; set; }
    public decimal PenaltyAmount { get; set; }
}

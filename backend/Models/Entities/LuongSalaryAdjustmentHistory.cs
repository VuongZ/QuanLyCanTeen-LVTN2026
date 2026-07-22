namespace LuanVanTotNghiep.backend.Models.Entities;

public class LuongSalaryAdjustmentHistory
{
    public int Id { get; set; }
    public int SalaryId { get; set; }
    public int UserId { get; set; }
    public int CreatedByUserId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal BonusAmount { get; set; }
    public decimal PenaltyAmount { get; set; }
    public string Reason { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public virtual LuongMonthlySalary Salary { get; set; } = null!;
    public virtual NsUser User { get; set; } = null!;
    public virtual NsUser CreatedByUser { get; set; } = null!;
}

namespace LuanVanTotNghiep.backend.Models.Entities;

public class LuongSalaryComplaint
{
    public int Id { get; set; }
    public int SalaryId { get; set; }
    public int UserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Status { get; set; } = "PENDING";
    public string? ManagerResponse { get; set; }
    public int? ReviewedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }

    public virtual LuongMonthlySalary Salary { get; set; } = null!;
    public virtual NsUser User { get; set; } = null!;
    public virtual NsUser? ReviewedByUser { get; set; }
}

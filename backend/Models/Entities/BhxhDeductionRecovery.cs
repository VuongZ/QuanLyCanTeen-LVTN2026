using System;

namespace LuanVanTotNghiep.backend.Models.Entities;

/// <summary>
/// Chi tiết thu hồi phần BHXH doanh nghiệp đã ứng trước
/// cho nhân viên ở các kỳ trước.
/// </summary>
public partial class BhxhDeductionRecovery
{
    public int Id { get; set; }

    public int UserId { get; set; }

    /// <summary>
    /// Khoản BHXH cũ đang còn số tiền chưa thu hồi.
    /// </summary>
    public int SourceContributionId { get; set; }

    /// <summary>
    /// Bảng lương tháng sau được sử dụng để thu hồi.
    /// </summary>
    public int RecoverySalaryId { get; set; }

    public decimal RecoveryAmount { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual NsUser User { get; set; } = null!;

    public virtual BhxhMonthlyContribution
        SourceContribution { get; set; } = null!;

    public virtual LuongMonthlySalary
        RecoverySalary { get; set; } = null!;
}

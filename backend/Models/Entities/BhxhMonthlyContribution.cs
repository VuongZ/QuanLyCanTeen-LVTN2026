using System;
using System.Collections.Generic;

namespace LuanVanTotNghiep.backend.Models.Entities;

/// <summary>
/// Khoản đóng BHXH của một nhân viên trong một tháng.
/// </summary>
public partial class BhxhMonthlyContribution
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int ProfileId { get; set; }

    public int? RateConfigId { get; set; }

    public sbyte Month { get; set; }

    public short Year { get; set; }

    public decimal InsuranceSalaryBasis { get; set; }

    public decimal EmployeeRate { get; set; }

    public decimal EmployerRate { get; set; }

    /// <summary>
    /// Tổng phần nhân viên phải đóng trong kỳ.
    /// </summary>
    public decimal EmployeeAmount { get; set; }

    /// <summary>
    /// Tổng số tiền đã khấu trừ từ lương nhân viên,
    /// bao gồm khấu trừ trong kỳ và thu hồi ở các kỳ sau.
    /// </summary>
    public decimal EmployeeDeductedAmount { get; set; }

    /// <summary>
    /// Phần nhân viên phải đóng nhưng doanh nghiệp đã ứng trước
    /// và chưa thu hồi được từ lương.
    /// </summary>
    public decimal EmployeeOutstandingAmount { get; set; }

    /// <summary>
    /// NONE / PARTIAL / FULL.
    /// </summary>
    public string DeductionStatus { get; set; } = "NONE";

    public decimal EmployerAmount { get; set; }

    public decimal TotalAmount { get; set; }

    /// <summary>
    /// DRAFT / CONFIRMED / PAID / CANCELLED.
    /// </summary>
    public string Status { get; set; } = "DRAFT";

    public int? ConfirmedByUserId { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    public int? PaidByUserId { get; set; }

    public DateTime? PaidAt { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual NsUser User { get; set; } = null!;

    public virtual BhxhEmployeeProfile Profile { get; set; } = null!;

    public virtual BhxhRateConfig? RateConfig { get; set; }

    public virtual NsUser? ConfirmedByUser { get; set; }

    public virtual NsUser? PaidByUser { get; set; }

    /// <summary>
    /// Bảng lương của chính kỳ phát sinh khoản đóng.
    /// </summary>
    public virtual LuongMonthlySalary? MonthlySalary { get; set; }

    /// <summary>
    /// Các lần thu hồi phần doanh nghiệp đã ứng trước.
    /// </summary>
    public virtual ICollection<BhxhDeductionRecovery>
        DeductionRecoveries { get; set; }
        = new List<BhxhDeductionRecovery>();
}

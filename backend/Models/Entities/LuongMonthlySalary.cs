using System;
using System.Collections.Generic;

namespace LuanVanTotNghiep.backend.Models.Entities;

public partial class LuongMonthlySalary
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int Month { get; set; }

    public int Year { get; set; }

    public decimal TotalHours { get; set; }

    public decimal HourlyWageAtTime { get; set; }

    /// <summary>
    /// Tổng lương trước khi trừ BHXH.
    /// Công thức: tiền giờ + thưởng - phạt.
    /// </summary>
    public decimal TotalSalary { get; set; }

    public decimal? TotalBonus { get; set; }

    public decimal? TotalPenalty { get; set; }

    /// <summary>
    /// ID khoản đóng BHXH phát sinh trong chính tháng lương.
    /// </summary>
    public int? BhxhContributionId { get; set; }

    /// <summary>
    /// Tổng khấu trừ BHXH trên bảng lương:
    /// BHXH tháng hiện tại + thu hồi phần ứng trước của các tháng cũ.
    /// </summary>
    public decimal SocialInsuranceDeduction { get; set; }

    /// <summary>
    /// Khoản BHXH phát sinh trong chính tháng bảng lương.
    /// </summary>
    public decimal CurrentBhxhDeduction { get; set; }

    /// <summary>
    /// Khoản doanh nghiệp đã ứng trước ở các tháng cũ
    /// được thu hồi trong bảng lương này.
    /// </summary>
    public decimal PreviousBhxhRecovery { get; set; }

    public string? Status { get; set; }

    public DateTime? PaidAt { get; set; }

    public DateTime? FinalizedAt { get; set; }

    public int? FinalizedByUserId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<CaAttendance>
        CaAttendances { get; set; }
        = new List<CaAttendance>();

    public virtual ICollection<LuongSalaryAdjustmentHistory>
        AdjustmentHistories { get; set; }
        = new List<LuongSalaryAdjustmentHistory>();

    public virtual ICollection<LuongSalaryComplaint>
        Complaints { get; set; }
        = new List<LuongSalaryComplaint>();

    public virtual NsUser? FinalizedByUser { get; set; }

    public virtual NsUser User { get; set; } = null!;

    /// <summary>
    /// Khoản đóng BHXH phát sinh trong chính tháng lương.
    /// </summary>
    public virtual BhxhMonthlyContribution?
        BhxhContribution { get; set; }

    /// <summary>
    /// Chi tiết các khoản BHXH cũ được thu hồi trong bảng lương này.
    /// </summary>
    public virtual ICollection<BhxhDeductionRecovery>
        BhxhDeductionRecoveries { get; set; }
        = new List<BhxhDeductionRecovery>();
}

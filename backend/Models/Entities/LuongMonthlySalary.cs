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

    ///  
    /// Tổng tiền lương trước khi trừ BHXH.
    ///
    /// Công thức hiện tại:
    /// Tiền giờ + thưởng - phạt.
    ///  
    public decimal TotalSalary { get; set; }

    public decimal? TotalBonus { get; set; }

    public decimal? TotalPenalty { get; set; }

    ///  
    /// ID khoản đóng BHXH được liên kết với bảng lương.
    ///
    /// Có thể NULL khi:
    /// - Nhân viên là PART_TIME.
    /// - Bảng lương chưa được chốt.
    /// - Chưa có khoản đóng BHXH được xác nhận.
    ///  
    public int? BhxhContributionId { get; set; }

    ///  
    /// Phần BHXH do nhân viên đóng,
    /// được khấu trừ khỏi lương thực nhận.
    ///
    /// Giá trị này được lấy từ:
    /// BhxhMonthlyContribution.EmployeeAmount.
    ///
    /// Không sử dụng EmployerAmount vì đó là
    /// phần doanh nghiệp phải đóng.
    ///  
    public decimal SocialInsuranceDeduction { get; set; }

    public string? Status { get; set; }

    public DateTime? PaidAt { get; set; }

    public DateTime? FinalizedAt { get; set; }

    public int? FinalizedByUserId { get; set; }

    public DateTime? CreatedAt { get; set; }

    // Các bản ghi điểm danh thuộc bảng lương.
    public virtual ICollection<CaAttendance>
        CaAttendances { get; set; }
        = new List<CaAttendance>();

    // Lịch sử điều chỉnh thưởng và phạt.
    public virtual ICollection<LuongSalaryAdjustmentHistory>
        AdjustmentHistories { get; set; }
        = new List<LuongSalaryAdjustmentHistory>();

    // Khiếu nại liên quan đến bảng lương.
    public virtual ICollection<LuongSalaryComplaint>
        Complaints { get; set; }
        = new List<LuongSalaryComplaint>();

    // Người đã chốt bảng lương.
    public virtual NsUser? FinalizedByUser { get; set; }

    // Nhân viên sở hữu bảng lương.
    public virtual NsUser User { get; set; } = null!;

    ///  
    /// Khoản đóng BHXH được sử dụng để khấu trừ
    /// cho bảng lương này.
    ///  
    public virtual BhxhMonthlyContribution?
        BhxhContribution { get; set; }
}
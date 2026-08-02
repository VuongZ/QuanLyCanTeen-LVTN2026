using System;

namespace LuanVanTotNghiep.backend.Models.Entities;

/// <summary>
/// Khoản đóng BHXH của một nhân viên trong một tháng.
///
/// Bảng này lưu snapshot:
/// - Mức lương làm căn cứ.
/// - Tỷ lệ nhân viên.
/// - Tỷ lệ doanh nghiệp.
/// - Số tiền đã tính.
///
/// Nhờ vậy dữ liệu lịch sử không thay đổi khi cấu hình tỷ lệ
/// hoặc mức lương đóng BHXH được cập nhật trong tương lai.
/// </summary>
public partial class BhxhMonthlyContribution
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int ProfileId { get; set; }

    /// <summary>
    /// Cấu hình tỷ lệ đã được dùng để tính.
    /// Có thể NULL nếu cấu hình cũ bị xóa liên kết.
    /// </summary>
    public int? RateConfigId { get; set; }

    /// <summary>
    /// Cột DB là TINYINT.
    /// Giá trị hợp lệ từ 1 đến 12.
    /// </summary>
    public sbyte Month { get; set; }

    /// <summary>
    /// Cột DB là SMALLINT.
    /// </summary>
    public short Year { get; set; }

    public decimal InsuranceSalaryBasis { get; set; }

    public decimal EmployeeRate { get; set; }

    public decimal EmployerRate { get; set; }

    public decimal EmployeeAmount { get; set; }

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

    // Nhân viên phát sinh khoản đóng.
    public virtual NsUser User { get; set; } = null!;

    // Hồ sơ BHXH được dùng để tạo khoản đóng.
    public virtual BhxhEmployeeProfile Profile { get; set; } = null!;

    // Cấu hình tỷ lệ đã dùng.
    public virtual BhxhRateConfig? RateConfig { get; set; }

    // Admin xác nhận khoản đóng.
    public virtual NsUser? ConfirmedByUser { get; set; }

    // Admin đánh dấu khoản đóng đã được nộp.
    public virtual NsUser? PaidByUser { get; set; }

    /// <summary>
    /// Bảng lương đã sử dụng khoản đóng BHXH này.
    ///
    /// Một khoản đóng BHXH chỉ được liên kết
    /// với tối đa một bảng lương tháng.
    /// </summary>
    public virtual LuongMonthlySalary? MonthlySalary { get; set; }
}
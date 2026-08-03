using System;

namespace LuanVanTotNghiep.DTOs;

/// <summary>
/// Dữ liệu bảng lương của một nhân viên trong một tháng.
/// </summary>
public class SalaryDto
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string? Username { get; set; }

    public string? FullName { get; set; }

    public int? BranchId { get; set; }

    public string? BranchName { get; set; }

    public string EmploymentType { get; set; } = "PART_TIME";

    public string? BankName { get; set; }

    public string? BankAccountNumber { get; set; }

    public string? BankAccountName { get; set; }

    public int Month { get; set; }

    public int Year { get; set; }

    public decimal TotalHours { get; set; }

    public decimal HourlyWageAtTime { get; set; }

    /// <summary>
    /// Tổng lương trước khi khấu trừ BHXH.
    ///
    /// Công thức hiện tại:
    /// tiền công theo giờ + thưởng - phạt.
    /// </summary>
    public decimal TotalSalary { get; set; }

    public decimal TotalBonus { get; set; }

    public decimal TotalPenalty { get; set; }

    // ========================================================
    // THÔNG TIN KHẤU TRỪ BHXH
    // ========================================================

    /// <summary>
    /// ID khoản đóng BHXH được sử dụng
    /// cho bảng lương tháng này.
    ///
    /// Giá trị NULL khi:
    /// - Nhân viên là PART_TIME.
    /// - Bảng lương chưa liên kết khoản đóng BHXH.
    /// </summary>
    public int? BhxhContributionId { get; set; }

    /// <summary>
    /// Phần BHXH do nhân viên đóng và bị khấu trừ
    /// khỏi lương thực nhận.
    ///
    /// Giá trị này được lấy từ:
    /// BhxhMonthlyContribution.EmployeeAmount.
    ///
    /// Không bao gồm phần doanh nghiệp đóng.
    /// </summary>
    public decimal SocialInsuranceDeduction { get; set; }

    /// <summary>
    /// Lương thực nhận sau khi trừ BHXH.
    ///
    /// Không cần lưu thêm vào database vì có thể tính từ:
    /// TotalSalary - SocialInsuranceDeduction.
    /// </summary>
    public decimal NetSalary =>
        Math.Max(
            0m,
            TotalSalary -
            SocialInsuranceDeduction
        );

    // ========================================================
    // TRẠNG THÁI BẢNG LƯƠNG
    // ========================================================

    public string? Status { get; set; }

    public DateTime? PaidAt { get; set; }

    public DateTime? FinalizedAt { get; set; }

    public int? FinalizedByUserId { get; set; }

    public string? FinalizedByName { get; set; }

    public DateTime? CreatedAt { get; set; }
}


/// <summary>
/// Dữ liệu tổng hợp tiền lương theo chi nhánh và kỳ lương.
///
/// Chưa thay đổi DTO này trong bước hiện tại.
/// Sau khi lương cá nhân khấu trừ BHXH hoạt động đúng,
/// mới bổ sung tổng BHXH và tổng tiền thực trả theo chi nhánh.
/// </summary>
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

   /// <summary>
/// Tổng số tiền thực nhận chưa được thanh toán.
/// Đã trừ phần BHXH nhân viên phải đóng.
/// </summary>
public decimal PendingTotal { get; set; }

/// <summary>
/// Tổng số tiền thực nhận đã thanh toán.
/// Đã trừ phần BHXH nhân viên phải đóng.
/// </summary>
public decimal PaidTotal { get; set; }

/// <summary>
/// Tổng lương trước khi khấu trừ BHXH.
/// </summary>
public decimal TotalSalary { get; set; }

/// <summary>
/// Tổng phần BHXH do nhân viên đóng.
/// </summary>
public decimal TotalSocialInsuranceDeduction { get; set; }

/// <summary>
/// Tổng tiền thực nhận sau khi trừ BHXH.
/// </summary>
public decimal TotalNetSalary { get; set; }

    public int PendingCount { get; set; }

    public int PaidCount { get; set; }

    public int EmployeeCount { get; set; }

    public int? TransferId { get; set; }

    public bool IsTransferred { get; set; }

    public decimal TransferredAmount { get; set; }

    public DateTime? TransferredAt { get; set; }

    public string? TransferredByName { get; set; }
}

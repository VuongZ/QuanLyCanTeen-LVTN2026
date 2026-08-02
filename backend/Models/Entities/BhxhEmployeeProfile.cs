using System;
using System.Collections.Generic;

namespace LuanVanTotNghiep.backend.Models.Entities;

/// <summary>
/// Hồ sơ BHXH hiện tại của một nhân viên FULL_TIME.
///
/// Quy tắc nghiệp vụ sẽ được kiểm tra ở Service:
/// - Chỉ nhân viên FULL_TIME mới được tạo hồ sơ.
/// - Mỗi nhân viên có tối đa một hồ sơ hiện tại.
/// - Mã số BHXH không được trùng.
/// </summary>
public partial class BhxhEmployeeProfile
{
    public int Id { get; set; }

    /// <summary>
    /// Nhân viên sở hữu hồ sơ.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Mã số BHXH của nhân viên.
    /// Có thể NULL khi hồ sơ đang chờ hoàn thiện.
    /// </summary>
    public string? SocialInsuranceNumber { get; set; }

    /// <summary>
    /// Mức lương làm căn cứ đóng BHXH.
    /// Không lấy trực tiếp từ tổng lương làm theo giờ trong tháng.
    /// </summary>
    public decimal InsuranceSalaryBasis { get; set; }

    /// <summary>
    /// Ngày bắt đầu tham gia BHXH.
    /// </summary>
    public DateOnly StartDate { get; set; }

    /// <summary>
    /// Ngày kết thúc tham gia.
    /// NULL nghĩa là vẫn đang tham gia hoặc chưa xác định ngày kết thúc.
    /// </summary>
    public DateOnly? EndDate { get; set; }

    /// <summary>
    /// PENDING / ACTIVE / SUSPENDED / STOPPED.
    /// </summary>
    public string Status { get; set; } = "PENDING";

    public string? Note { get; set; }

    public int? CreatedByUserId { get; set; }

    public int? UpdatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Nhân viên sở hữu hồ sơ.
    public virtual NsUser User { get; set; } = null!;

    // Admin tạo hồ sơ.
    public virtual NsUser? CreatedByUser { get; set; }

    // Admin cập nhật hồ sơ gần nhất.
    public virtual NsUser? UpdatedByUser { get; set; }

    // Lịch sử các khoản đóng theo tháng của hồ sơ.
    public virtual ICollection<BhxhMonthlyContribution>
        BhxhMonthlyContributions { get; set; }
        = new List<BhxhMonthlyContribution>();
}
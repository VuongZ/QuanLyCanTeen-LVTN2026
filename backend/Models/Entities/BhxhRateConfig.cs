using System;
using System.Collections.Generic;

namespace LuanVanTotNghiep.backend.Models.Entities;

/// <summary>
/// Cấu hình tỷ lệ đóng bảo hiểm xã hội theo thời gian hiệu lực.
///
/// Ví dụ:
/// - Nhân viên đóng 8%.
/// - Doanh nghiệp đóng 17,5%.
/// - Có hiệu lực từ ngày 01/01/2026.
///
/// Không viết cố định tỷ lệ trong Service để khi chính sách thay đổi
/// chỉ cần thêm cấu hình mới mà không phải sửa mã nguồn.
/// </summary>
public partial class BhxhRateConfig
{
    public int Id { get; set; }

    /// <summary>
    /// Tỷ lệ phần trăm do nhân viên đóng.
    /// Ví dụ 8.00 tương ứng 8%.
    /// </summary>
    public decimal EmployeeRate { get; set; }

    /// <summary>
    /// Tỷ lệ phần trăm do doanh nghiệp đóng.
    /// Ví dụ 17.50 tương ứng 17,5%.
    /// </summary>
    public decimal EmployerRate { get; set; }

    /// <summary>
    /// Ngày bắt đầu áp dụng cấu hình.
    /// </summary>
    public DateOnly EffectiveFrom { get; set; }

    /// <summary>
    /// Ngày kết thúc áp dụng.
    /// NULL nghĩa là chưa xác định ngày kết thúc.
    /// </summary>
    public DateOnly? EffectiveTo { get; set; }

    /// <summary>
    /// Cấu hình còn được phép sử dụng hay không.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Admin đã tạo cấu hình.
    /// Có thể NULL đối với dữ liệu khởi tạo từ SQL.
    /// </summary>
    public int? CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Navigation tới Admin tạo cấu hình.
    public virtual NsUser? CreatedByUser { get; set; }

    // Các khoản đóng tháng đã sử dụng cấu hình này.
    public virtual ICollection<BhxhMonthlyContribution>
        BhxhMonthlyContributions { get; set; }
        = new List<BhxhMonthlyContribution>();
}
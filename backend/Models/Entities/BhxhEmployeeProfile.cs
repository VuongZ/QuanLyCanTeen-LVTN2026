using System;
using System.Collections.Generic;

namespace LuanVanTotNghiep.backend.Models.Entities;

// Hồ sơ BHXH hiện tại của một nhân viên FULL_TIME.
//
// Quy tắc nghiệp vụ được kiểm tra ở Service:
// - Chỉ nhân viên FULL_TIME mới được tạo hồ sơ.
// - Mỗi nhân viên có tối đa một hồ sơ hiện tại.
// - Mã số BHXH không được trùng.
// - Staff phải xác nhận hồ sơ trước khi Admin kích hoạt.
public partial class BhxhEmployeeProfile
{
    public int Id { get; set; }

    // Nhân viên sở hữu hồ sơ BHXH.
    public int UserId { get; set; }

    // Mã số BHXH của nhân viên.
    // Có thể NULL khi hồ sơ đang chờ hoàn thiện.
    public string? SocialInsuranceNumber { get; set; }

    // Mức lương làm căn cứ đóng BHXH.
    // Không lấy trực tiếp từ tổng lương làm theo giờ trong tháng.
    public decimal InsuranceSalaryBasis { get; set; }

    // Ngày bắt đầu tham gia BHXH.
    public DateOnly StartDate { get; set; }

    // Ngày kết thúc tham gia.
    // NULL nghĩa là chưa xác định ngày kết thúc.
    public DateOnly? EndDate { get; set; }

    // Trạng thái do Admin quản lý:
    // PENDING / ACTIVE / SUSPENDED / STOPPED.
    public string Status { get; set; } = "PENDING";

    // Trạng thái Staff kiểm tra hồ sơ:
    // PENDING: Staff chưa kiểm tra.
    // CONFIRMED: Staff xác nhận thông tin đúng.
    // CHANGE_REQUESTED: Staff yêu cầu Admin chỉnh sửa.
    public string StaffConfirmationStatus { get; set; }
        = "PENDING";

    // Thời điểm Staff xác nhận hồ sơ.
    // NULL khi Staff chưa xác nhận
    // hoặc khi Staff yêu cầu chỉnh sửa.
    public DateTime? StaffConfirmedAt { get; set; }

    // Nội dung phản hồi của Staff.
    // Ví dụ: "Mã số BHXH của tôi chưa chính xác."
    public string? StaffConfirmationNote { get; set; }

    // Ghi chú nội bộ của Admin.
    public string? Note { get; set; }

    // ID Admin tạo hồ sơ.
    public int? CreatedByUserId { get; set; }

    // ID Admin cập nhật hồ sơ gần nhất.
    public int? UpdatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Nhân viên sở hữu hồ sơ.
    public virtual NsUser User { get; set; } = null!;

    // Admin tạo hồ sơ.
    public virtual NsUser? CreatedByUser { get; set; }

    // Admin cập nhật hồ sơ gần nhất.
    public virtual NsUser? UpdatedByUser { get; set; }

    // Các khoản đóng BHXH hằng tháng.
    public virtual ICollection<BhxhMonthlyContribution>
        BhxhMonthlyContributions { get; set; }
        = new List<BhxhMonthlyContribution>();
}
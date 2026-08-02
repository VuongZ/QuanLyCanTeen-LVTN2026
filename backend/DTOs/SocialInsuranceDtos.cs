using System;
using System.ComponentModel.DataAnnotations;

namespace LuanVanTotNghiep.DTOs;

// ============================================================
// 1. DTO DANH SÁCH NHÂN VIÊN FULL_TIME
// ============================================================

///  
/// Dùng để hiển thị danh sách nhân viên FULL_TIME
/// cho Admin lựa chọn khi tạo hồ sơ BHXH.
///
/// DTO không trả về mật khẩu hoặc những thông tin
/// không cần thiết của Entity NsUser.
///  
public class SocialInsuranceFullTimeEmployeeDto
{
    // ID của nhân viên trong bảng ns_user.
    public int UserId { get; set; }

    // Họ tên nhân viên.
    public string FullName { get; set; } = string.Empty;

    // Email nhân viên.
    public string Email { get; set; } = string.Empty;

    // Số điện thoại có thể không có.
    public string? PhoneNumber { get; set; }

    // Loại nhân viên, dự kiến là FULL_TIME.
    public string EmploymentType { get; set; } = string.Empty;

    // Ngày nhân viên bắt đầu làm việc.
    public DateOnly? HireDate { get; set; }

    ///  
    /// true: nhân viên đã có hồ sơ BHXH.
    /// false: nhân viên chưa có hồ sơ BHXH.
    ///  
    public bool HasSocialInsuranceProfile { get; set; }

    ///  
    /// Trạng thái hồ sơ hiện tại:
    /// PENDING, ACTIVE, SUSPENDED hoặc STOPPED.
    ///  
    public string? ProfileStatus { get; set; }
}


// ============================================================
// 2. DTO CẤU HÌNH TỶ LỆ ĐÓNG BHXH
// ============================================================

///  
/// Dữ liệu cấu hình tỷ lệ BHXH trả về cho giao diện.
///  
public class BhxhRateConfigDto
{
    public int Id { get; set; }

    // Tỷ lệ nhân viên đóng, ví dụ 8.00%.
    public decimal EmployeeRate { get; set; }

    // Tỷ lệ doanh nghiệp đóng, ví dụ 17.50%.
    public decimal EmployerRate { get; set; }

    // Ngày bắt đầu áp dụng tỷ lệ.
    public DateOnly EffectiveFrom { get; set; }

    // Ngày kết thúc áp dụng, có thể để trống.
    public DateOnly? EffectiveTo { get; set; }


    // Cấu hình còn được sử dụng hay không.
    public bool IsActive { get; set; }

/// Cho biết cấu hình đã từng được dùng
/// để sinh khoản đóng BHXH hay chưa.

public bool HasBeenUsed { get; set; }


/// Cho biết Admin có thể sửa trực tiếp
/// cấu hình này hay không.
///
/// Chỉ true khi:
/// - Cấu hình vẫn hoạt động.
/// - Chưa đến ngày hiệu lực.
/// - Chưa được dùng để sinh khoản đóng.

public bool CanEdit { get; set; }

    // Admin đã tạo cấu hình.
    public int? CreatedByUserId { get; set; }

    public string? CreatedByUserName { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

///  
/// Dữ liệu Admin gửi lên khi tạo một cấu hình tỷ lệ mới.
///  
public class CreateBhxhRateConfigRequest
{
    ///  
    /// Tỷ lệ nhập theo phần trăm.
    /// Ví dụ nhập 8 nghĩa là 8%.
    ///  
    [Range(
        typeof(decimal),
        "0",
        "100",
        ErrorMessage =
            "Tỷ lệ nhân viên phải nằm trong khoảng từ 0 đến 100.")]
    public decimal EmployeeRate { get; set; }

    ///  
    /// Tỷ lệ doanh nghiệp đóng.
    /// Ví dụ nhập 17.5 nghĩa là 17,5%.
    ///  
    [Range(
        typeof(decimal),
        "0",
        "100",
        ErrorMessage =
            "Tỷ lệ doanh nghiệp phải nằm trong khoảng từ 0 đến 100.")]
    public decimal EmployerRate { get; set; }

    // Ngày bắt đầu áp dụng cấu hình.
    public DateOnly EffectiveFrom { get; set; }

    ///  
    /// Có thể để trống nếu chưa xác định ngày kết thúc.
    /// Service sẽ kiểm tra ngày kết thúc không được
    /// nhỏ hơn ngày bắt đầu.
    ///  
    public DateOnly? EffectiveTo { get; set; }
}


/// Dữ liệu Admin gửi lên khi cập nhật
/// một cấu hình tỷ lệ BHXH.
///
/// Chỉ cấu hình chưa đến ngày hiệu lực
/// và chưa từng được sử dụng mới được sửa.

public class UpdateBhxhRateConfigRequest
{

    /// Tỷ lệ nhân viên đóng.
    ///
    /// Ví dụ:
    /// 8 nghĩa là 8%.

    [Range(
        typeof(decimal),
        "0",
        "100",
        ErrorMessage =
            "Tỷ lệ nhân viên phải nằm trong khoảng từ 0 đến 100.")]
    public decimal EmployeeRate { get; set; }


    /// Tỷ lệ doanh nghiệp đóng.
    ///
    /// Ví dụ:
    /// 17.5 nghĩa là 17,5%.

    [Range(
        typeof(decimal),
        "0",
        "100",
        ErrorMessage =
            "Tỷ lệ doanh nghiệp phải nằm trong khoảng từ 0 đến 100.")]
    public decimal EmployerRate { get; set; }

  
    /// Ngày bắt đầu áp dụng cấu hình.
    /// Ph EmployerRate { get; set; }


    /// Ngày bắt đầu áp dụng cấu hình.
    /// Phải là một ngày trong tương lai.

    public DateOnly EffectiveFrom { get; set; }


    /// Ngày kết thúc áp dụng.
    /// Có thể để trống.
 
    public DateOnly? EffectiveTo { get; set; }
}


// ============================================================
// 3. DTO HỒ SƠ BHXH CỦA NHÂN VIÊN
// ============================================================

///  
/// Dữ liệu hồ sơ BHXH trả về cho Admin hoặc nhân viên.
///  
public class BhxhEmployeeProfileDto
{
    // ID hồ sơ BHXH.
    public int Id { get; set; }

    // ID nhân viên sở hữu hồ sơ.
    public int UserId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string EmploymentType { get; set; } = string.Empty;

    // Mã số BHXH có thể trống khi hồ sơ đang PENDING.
    public string? SocialInsuranceNumber { get; set; }

    // Mức lương làm căn cứ đóng BHXH.
    public decimal InsuranceSalaryBasis { get; set; }

    // Ngày bắt đầu tham gia BHXH.
    public DateOnly StartDate { get; set; }

    // Ngày kết thúc tham gia, có thể để trống.
    public DateOnly? EndDate { get; set; }

    // PENDING, ACTIVE, SUSPENDED hoặc STOPPED.
    public string Status { get; set; } = string.Empty;

    public string? Note { get; set; }

    // Admin tạo hồ sơ.
    public int? CreatedByUserId { get; set; }

    public string? CreatedByUserName { get; set; }

    // Admin cập nhật hồ sơ gần nhất.
    public int? UpdatedByUserId { get; set; }

    public string? UpdatedByUserName { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

///  
/// Dữ liệu Admin gửi lên khi tạo hồ sơ BHXH.
///
/// Quy tắc chỉ FULL_TIME mới được tạo hồ sơ
/// sẽ được kiểm tra lại trong Service.
///  
public class CreateBhxhEmployeeProfileRequest
{
    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "Nhân viên không hợp lệ.")]
    public int UserId { get; set; }

    ///  
    /// Có thể để trống khi hồ sơ mới ở trạng thái PENDING.
///  
    [StringLength(
        20,
        ErrorMessage =
            "Mã số BHXH không được vượt quá 20 ký tự.")]
    public string? SocialInsuranceNumber { get; set; }

    [Range(
        typeof(decimal),
        "0.01",
        "9999999999999.99",
        ErrorMessage =
            "Mức lương làm căn cứ đóng phải lớn hơn 0.")]
    public decimal InsuranceSalaryBasis { get; set; }

    // Ngày bắt đầu tham gia BHXH.
    public DateOnly StartDate { get; set; }

    // Có thể để trống khi nhân viên vẫn đang tham gia.
    public DateOnly? EndDate { get; set; }

    [StringLength(
        500,
        ErrorMessage =
            "Ghi chú không được vượt quá 500 ký tự.")]
    public string? Note { get; set; }
}

///  
/// Dữ liệu Admin gửi lên khi cập nhật hồ sơ.
///
/// Không cho sửa UserId để tránh chuyển hồ sơ
/// của nhân viên này sang nhân viên khác.
///  
public class UpdateBhxhEmployeeProfileRequest
{
    [StringLength(
        20,
        ErrorMessage =
            "Mã số BHXH không được vượt quá 20 ký tự.")]
    public string? SocialInsuranceNumber { get; set; }

    [Range(
        typeof(decimal),
        "0.01",
        "9999999999999.99",
        ErrorMessage =
            "Mức lương làm căn cứ đóng phải lớn hơn 0.")]
    public decimal InsuranceSalaryBasis { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    [StringLength(
        500,
        ErrorMessage =
            "Ghi chú không được vượt quá 500 ký tự.")]
    public string? Note { get; set; }
}

///  
/// Dùng riêng cho chức năng đổi trạng thái hồ sơ.
///  
public class UpdateBhxhProfileStatusRequest
{
    ///  
    /// Các giá trị hợp lệ:
    /// PENDING, ACTIVE, SUSPENDED, STOPPED.
    ///
    /// Service vẫn phải kiểm tra lại danh sách trạng thái.
///  
    [Required(ErrorMessage = "Trạng thái hồ sơ là bắt buộc.")]
    [StringLength(
        20,
        ErrorMessage = "Trạng thái hồ sơ không hợp lệ.")]
    public string Status { get; set; } = string.Empty;

    [StringLength(
        500,
        ErrorMessage =
            "Ghi chú không được vượt quá 500 ký tự.")]
    public string? Note { get; set; }
}


// ============================================================
// 4. DTO KHOẢN ĐÓNG BHXH HẰNG THÁNG
// ============================================================

///  
/// Dữ liệu Admin gửi lên để sinh khoản đóng BHXH
/// cho các hồ sơ ACTIVE trong một tháng.
///  
public class GenerateBhxhContributionsRequest
{
    [Range(
        1,
        12,
        ErrorMessage =
            "Tháng phải nằm trong khoảng từ 1 đến 12.")]
    public int Month { get; set; }

    [Range(
        2000,
        2100,
        ErrorMessage = "Năm không hợp lệ.")]
    public int Year { get; set; }
}

///  
/// Dữ liệu khoản đóng BHXH trả về cho giao diện.
///
/// Các trường mức lương và tỷ lệ là dữ liệu snapshot,
/// tức là được lưu cố định tại tháng phát sinh.
///  
public class BhxhMonthlyContributionDto
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public int ProfileId { get; set; }

    public int? RateConfigId { get; set; }

    public int Month { get; set; }

    public int Year { get; set; }

    // Mức lương làm căn cứ tại tháng được tính.
    public decimal InsuranceSalaryBasis { get; set; }

    // Tỷ lệ nhân viên tại thời điểm tính.
    public decimal EmployeeRate { get; set; }

    // Tỷ lệ doanh nghiệp tại thời điểm tính.
    public decimal EmployerRate { get; set; }

    // Tiền nhân viên phải đóng.
    public decimal EmployeeAmount { get; set; }

    // Tiền doanh nghiệp phải đóng.
    public decimal EmployerAmount { get; set; }

    // Tổng tiền đóng của cả hai bên.
    public decimal TotalAmount { get; set; }

    // DRAFT, CONFIRMED, PAID hoặc CANCELLED.
    public string Status { get; set; } = string.Empty;

    // Admin xác nhận khoản đóng.
    public int? ConfirmedByUserId { get; set; }

    public string? ConfirmedByUserName { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    // Admin đánh dấu khoản đóng đã được nộp.
    public int? PaidByUserId { get; set; }

    public string? PaidByUserName { get; set; }

    public DateTime? PaidAt { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

///  
/// Kết quả sau khi Admin yêu cầu sinh khoản đóng
/// cho một tháng.
///  
public class GenerateBhxhContributionsResultDto
{
    public int Month { get; set; }

    public int Year { get; set; }

    ///  
    /// Số khoản đóng mới đã được tạo.
///  
    public int CreatedCount { get; set; }

    ///  
    /// Số hồ sơ bị bỏ qua vì tháng đó
    /// đã có khoản đóng trước đó.
///  
    public int SkippedExistingCount { get; set; }

    ///  
    /// Số hồ sơ bị bỏ qua vì tháng được chọn
    /// không nằm trong thời gian tham gia BHXH.
///  
    public int SkippedOutOfPeriodCount { get; set; }

    public string Message { get; set; } = string.Empty;
}
// ============================================================
// 5. DTO NGỪNG CẤU HÌNH VÀ HỦY KHOẢN ĐÓNG
// ============================================================

///  
/// Dữ liệu Admin gửi lên khi ngừng sử dụng
/// một cấu hình tỷ lệ BHXH.
///
/// Hệ thống không xóa bản ghi cấu hình.
/// Chỉ cập nhật IsActive = false và EffectiveTo.
///  
public class DeactivateBhxhRateConfigRequest
{
    ///  
    /// Ngày cuối cùng cấu hình còn hiệu lực.
    ///
    /// Ví dụ:
    /// Cấu hình mới áp dụng từ 01/09/2026
    /// thì cấu hình cũ có thể kết thúc ngày 31/08/2026.
    ///  
    public DateOnly EffectiveTo { get; set; }
}

///  
/// Dữ liệu Admin gửi lên khi hủy một khoản đóng BHXH.
///
/// Khoản đóng không bị xóa khỏi database.
/// Trạng thái sẽ được chuyển sang CANCELLED.
///  
public class CancelBhxhContributionRequest
{
    [Required(
        ErrorMessage = "Lý do hủy khoản đóng là bắt buộc.")]
    [StringLength(
        500,
        ErrorMessage =
            "Lý do hủy không được vượt quá 500 ký tự.")]
    public string Reason { get; set; } = string.Empty;
}
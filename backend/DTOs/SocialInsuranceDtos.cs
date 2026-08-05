using System;
using System.ComponentModel.DataAnnotations;

namespace LuanVanTotNghiep.DTOs;


// ============================================================
// 1. DTO DANH SÁCH NHÂN VIÊN FULL_TIME
// ============================================================

// Dùng để hiển thị danh sách nhân viên FULL_TIME
// cho Admin lựa chọn khi tạo hồ sơ BHXH.
//
// DTO không trả mật khẩu hoặc các thông tin
// không cần thiết của tài khoản nhân viên.
public class SocialInsuranceFullTimeEmployeeDto
{
    // ID nhân viên trong bảng ns_user.
    public int UserId { get; set; }

    // Họ tên nhân viên.
    public string FullName { get; set; }
        = string.Empty;

    // Email nhân viên.
    public string Email { get; set; }
        = string.Empty;

    // Số điện thoại có thể để trống.
    public string? PhoneNumber { get; set; }

    // Theo nghiệp vụ nhóm, giá trị phải là FULL_TIME.
    public string EmploymentType { get; set; }
        = string.Empty;

    // Ngày nhân viên bắt đầu làm việc.
    public DateOnly? HireDate { get; set; }

    // true: nhân viên đã có hồ sơ BHXH.
    // false: nhân viên chưa có hồ sơ BHXH.
    public bool HasSocialInsuranceProfile { get; set; }

    // Trạng thái hồ sơ hiện tại:
    // PENDING, ACTIVE, SUSPENDED hoặc STOPPED.
    public string? ProfileStatus { get; set; }
}


// ============================================================
// 2. DTO CẤU HÌNH TỶ LỆ ĐÓNG BHXH
// ============================================================

// Dữ liệu cấu hình tỷ lệ đóng BHXH
// được trả về cho giao diện.
public class BhxhRateConfigDto
{
    public int Id { get; set; }

    // Tỷ lệ nhân viên đóng.
    // Ví dụ: 8.00 nghĩa là 8%.
    public decimal EmployeeRate { get; set; }

    // Tỷ lệ doanh nghiệp đóng.
    // Ví dụ: 17.50 nghĩa là 17,5%.
    public decimal EmployerRate { get; set; }

    // Ngày bắt đầu áp dụng cấu hình.
    public DateOnly EffectiveFrom { get; set; }

    // Ngày kết thúc áp dụng.
    // NULL nghĩa là chưa xác định ngày kết thúc.
    public DateOnly? EffectiveTo { get; set; }

    // Cấu hình hiện còn được sử dụng hay không.
    public bool IsActive { get; set; }

    // true khi cấu hình đã từng được dùng
    // để sinh ít nhất một khoản đóng BHXH.
    public bool HasBeenUsed { get; set; }

    // Admin chỉ được sửa cấu hình trực tiếp khi:
    // - Cấu hình vẫn hoạt động.
    // - Chưa đến ngày hiệu lực.
    // - Chưa được sử dụng để sinh khoản đóng.
    public bool CanEdit { get; set; }

    // Admin đã tạo cấu hình.
    public int? CreatedByUserId { get; set; }

    public string? CreatedByUserName { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}


// Dữ liệu Admin gửi lên khi tạo cấu hình tỷ lệ mới.
public class CreateBhxhRateConfigRequest
{
    // Tỷ lệ nhập theo phần trăm.
    // Ví dụ nhập 8 nghĩa là 8%.
    [Range(
        typeof(decimal),
        "0",
        "100",
        ErrorMessage =
            "Tỷ lệ nhân viên phải nằm trong khoảng từ 0 đến 100.")]
    public decimal EmployeeRate { get; set; }

    // Tỷ lệ doanh nghiệp đóng.
    // Ví dụ nhập 17.5 nghĩa là 17,5%.
    [Range(
        typeof(decimal),
        "0",
        "100",
        ErrorMessage =
            "Tỷ lệ doanh nghiệp phải nằm trong khoảng từ 0 đến 100.")]
    public decimal EmployerRate { get; set; }

    // Ngày bắt đầu áp dụng cấu hình.
    public DateOnly EffectiveFrom { get; set; }

    // Ngày kết thúc áp dụng.
    // Có thể để trống.
    public DateOnly? EffectiveTo { get; set; }
}


// Dữ liệu Admin gửi lên khi cập nhật
// một cấu hình tỷ lệ BHXH.
//
// Chỉ cấu hình chưa đến ngày hiệu lực
// và chưa được sử dụng mới được chỉnh sửa.
public class UpdateBhxhRateConfigRequest
{
    [Range(
        typeof(decimal),
        "0",
        "100",
        ErrorMessage =
            "Tỷ lệ nhân viên phải nằm trong khoảng từ 0 đến 100.")]
    public decimal EmployeeRate { get; set; }

    [Range(
        typeof(decimal),
        "0",
        "100",
        ErrorMessage =
            "Tỷ lệ doanh nghiệp phải nằm trong khoảng từ 0 đến 100.")]
    public decimal EmployerRate { get; set; }

    // Ngày bắt đầu áp dụng cấu hình.
    public DateOnly EffectiveFrom { get; set; }

    // Ngày kết thúc áp dụng.
    // Có thể để trống.
    public DateOnly? EffectiveTo { get; set; }
}


// ============================================================
// 3. DTO HỒ SƠ BHXH CỦA NHÂN VIÊN
// ============================================================

// Dữ liệu hồ sơ BHXH trả về cho Admin hoặc Staff.
public class BhxhEmployeeProfileDto
{
    // ID hồ sơ BHXH.
    public int Id { get; set; }

    // ID nhân viên sở hữu hồ sơ.
    public int UserId { get; set; }

    public string FullName { get; set; }
        = string.Empty;

    public string Email { get; set; }
        = string.Empty;

    public string EmploymentType { get; set; }
        = string.Empty;

    // Mã số BHXH có thể để trống
    // khi hồ sơ đang chờ hoàn thiện.
    public string? SocialInsuranceNumber { get; set; }

    // Mức lương làm căn cứ đóng BHXH.
    public decimal InsuranceSalaryBasis { get; set; }

    // Ngày bắt đầu tham gia BHXH.
    public DateOnly StartDate { get; set; }

    // Ngày kết thúc tham gia.
    // Có thể để trống.
    public DateOnly? EndDate { get; set; }

    // Trạng thái do Admin quản lý:
    // PENDING, ACTIVE, SUSPENDED hoặc STOPPED.
    public string Status { get; set; }
        = string.Empty;

    // Trạng thái Staff kiểm tra hồ sơ:
    // PENDING, CONFIRMED hoặc CHANGE_REQUESTED.
    public string StaffConfirmationStatus { get; set; }
        = "PENDING";

    // Thời điểm Staff xác nhận thông tin hồ sơ.
    public DateTime? StaffConfirmedAt { get; set; }

    // Phản hồi của Staff.
    //
    // Trường này thường có nội dung khi Staff
    // chọn yêu cầu Admin chỉnh sửa hồ sơ.
    public string? StaffConfirmationNote { get; set; }

    // Ghi chú nội bộ của Admin.
    public string? Note { get; set; }

    // Admin đã tạo hồ sơ.
    public int? CreatedByUserId { get; set; }

    public string? CreatedByUserName { get; set; }

    // Admin đã cập nhật hồ sơ gần nhất.
    public int? UpdatedByUserId { get; set; }

    public string? UpdatedByUserName { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}


// Dữ liệu Admin gửi lên khi tạo hồ sơ BHXH.
//
// Quy tắc chỉ FULL_TIME mới được tạo hồ sơ
// sẽ được Service kiểm tra lại.
public class CreateBhxhEmployeeProfileRequest
{
    [Range(
        1,
        int.MaxValue,
        ErrorMessage =
            "Nhân viên không hợp lệ.")]
    public int UserId { get; set; }

    // Có thể để trống khi hồ sơ mới
    // vẫn đang ở trạng thái PENDING.
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

    // Có thể để trống khi nhân viên
    // vẫn đang tham gia BHXH.
    public DateOnly? EndDate { get; set; }

    [StringLength(
        500,
        ErrorMessage =
            "Ghi chú không được vượt quá 500 ký tự.")]
    public string? Note { get; set; }
}


// Dữ liệu Admin gửi lên khi cập nhật hồ sơ.
//
// Không có UserId vì không cho phép chuyển hồ sơ
// từ nhân viên này sang nhân viên khác.
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


// Dùng riêng cho chức năng Admin đổi trạng thái hồ sơ.
public class UpdateBhxhProfileStatusRequest
{
    // Các giá trị hợp lệ:
    // PENDING, ACTIVE, SUSPENDED, STOPPED.
    //
    // Service vẫn phải kiểm tra lại
    // trước khi cập nhật database.
    [Required(
        ErrorMessage =
            "Trạng thái hồ sơ là bắt buộc.")]
    [StringLength(
        20,
        ErrorMessage =
            "Trạng thái hồ sơ không hợp lệ.")]
    public string Status { get; set; }
        = string.Empty;

    [StringLength(
        500,
        ErrorMessage =
            "Ghi chú không được vượt quá 500 ký tự.")]
    public string? Note { get; set; }
}


// Dữ liệu Staff gửi lên khi kiểm tra
// hồ sơ BHXH của chính mình.
//
// Staff chỉ được sử dụng hai trạng thái:
// - CONFIRMED
// - CHANGE_REQUESTED
public class UpdateMyBhxhConfirmationRequest
{
    [Required(
        ErrorMessage =
            "Trạng thái xác nhận là bắt buộc.")]
    [StringLength(
        30,
        ErrorMessage =
            "Trạng thái xác nhận không hợp lệ.")]
    public string ConfirmationStatus { get; set; }
        = string.Empty;

    // Staff phải nhập nội dung này
    // khi chọn CHANGE_REQUESTED.
    [StringLength(
        500,
        ErrorMessage =
            "Nội dung phản hồi không được vượt quá 500 ký tự.")]
    public string? Note { get; set; }
}


// ============================================================
// 4. DTO KHOẢN ĐÓNG BHXH HẰNG THÁNG
// ============================================================

// Dữ liệu Admin gửi lên để sinh khoản đóng BHXH
// cho các hồ sơ ACTIVE trong một tháng.
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
        ErrorMessage =
            "Năm không hợp lệ.")]
    public int Year { get; set; }
}


// Dữ liệu khoản đóng BHXH trả về cho giao diện.
//
// Các trường mức lương và tỷ lệ là dữ liệu snapshot,
// được giữ cố định theo tháng phát sinh.
public class BhxhMonthlyContributionDto
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string FullName { get; set; }
        = string.Empty;

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
    // Số tiền đã trừ thực tế từ lương nhân viên.
public decimal EmployeeDeductedAmount { get; set; }

// Số tiền doanh nghiệp tạm ứng cho phần nhân viên còn thiếu.
public decimal EmployeeOutstandingAmount { get; set; }

// NONE / PARTIAL / FULL.
public string DeductionStatus { get; set; }
    = "NONE";

    // Tiền doanh nghiệp phải đóng.
    public decimal EmployerAmount { get; set; }

    // Tổng tiền đóng của cả hai bên.
    public decimal TotalAmount { get; set; }

    // DRAFT, CONFIRMED, PAID hoặc CANCELLED.
    public string Status { get; set; }
        = string.Empty;

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


// Kết quả sau khi Admin yêu cầu sinh khoản đóng
// cho một tháng.
public class GenerateBhxhContributionsResultDto
{
    public int Month { get; set; }

    public int Year { get; set; }

    // Số khoản đóng mới đã được tạo.
    public int CreatedCount { get; set; }

    // Số hồ sơ bị bỏ qua vì tháng đó
    // đã có khoản đóng trước đó.
    public int SkippedExistingCount { get; set; }

    // Số hồ sơ bị bỏ qua vì tháng được chọn
    // không nằm trong thời gian tham gia BHXH.
    public int SkippedOutOfPeriodCount { get; set; }

    public string Message { get; set; }
        = string.Empty;
}


// ============================================================
// 5. DTO NGỪNG CẤU HÌNH VÀ HỦY KHOẢN ĐÓNG
// ============================================================

// Dữ liệu Admin gửi lên khi ngừng sử dụng
// một cấu hình tỷ lệ BHXH.
//
// Hệ thống không xóa bản ghi cấu hình.
// Chỉ cập nhật IsActive và EffectiveTo.
public class DeactivateBhxhRateConfigRequest
{
    // Ngày cuối cùng cấu hình còn hiệu lực.
    public DateOnly EffectiveTo { get; set; }
}


// Dữ liệu Admin gửi lên khi hủy một khoản đóng.
//
// Khoản đóng không bị xóa khỏi database.
// Trạng thái sẽ chuyển thành CANCELLED.
public class CancelBhxhContributionRequest
{
    [Required(
        ErrorMessage =
            "Lý do hủy khoản đóng là bắt buộc.")]
    [StringLength(
        500,
        ErrorMessage =
            "Lý do hủy không được vượt quá 500 ký tự.")]
    public string Reason { get; set; }
        = string.Empty;
}
using LuanVanTotNghiep.DTOs;

namespace LuanVanTotNghiep.Services;

///  
/// Khai báo các chức năng nghiệp vụ của phân hệ BHXH.
///
/// Interface chỉ mô tả Service có thể làm những gì.
/// Phần kiểm tra nghiệp vụ và tính toán thật sẽ được viết
/// trong class SocialInsuranceService.
///  
public interface ISocialInsuranceService
{
    // ========================================================
    // 1. NHÂN VIÊN FULL_TIME
    // ========================================================

    ///  
    /// Lấy danh sách nhân viên FULL_TIME chưa bị xóa.
    ///
    /// Kết quả cho biết mỗi nhân viên:
    /// - Đã có hồ sơ BHXH hay chưa.
    /// - Hồ sơ hiện tại có trạng thái gì.
    ///  
    Task<List<SocialInsuranceFullTimeEmployeeDto>>
        GetFullTimeEmployeesAsync();


    // ========================================================
    // 2. CẤU HÌNH TỶ LỆ ĐÓNG BHXH
    // ========================================================

    ///  
    /// Lấy toàn bộ lịch sử cấu hình tỷ lệ đóng BHXH.
    ///  
    Task<List<BhxhRateConfigDto>>
        GetAllRateConfigsAsync();

    ///  
    /// Tạo một cấu hình tỷ lệ đóng BHXH mới.
    ///
    /// Service sẽ kiểm tra:
    /// - Tỷ lệ có hợp lệ không.
    /// - Ngày kết thúc có trước ngày bắt đầu không.
    /// - Có bị trùng ngày bắt đầu hiệu lực không.
    ///  
    Task<BhxhRateConfigDto> CreateRateConfigAsync(
        CreateBhxhRateConfigRequest request,
        int adminUserId);

/// Cập nhật cấu hình tỷ lệ đóng BHXH.
///
/// Chỉ cho phép chỉnh sửa khi đồng thời:
/// - Cấu hình vẫn đang hoạt động.
/// - Chưa đến ngày bắt đầu hiệu lực.
/// - Chưa được dùng để sinh khoản đóng BHXH.
///
/// Nếu không đủ điều kiện, Admin phải ngừng
/// cấu hình cũ và tạo cấu hình mới.
Task<BhxhRateConfigDto> UpdateRateConfigAsync(
    int rateConfigId,
    UpdateBhxhRateConfigRequest request,
    int adminUserId);

    ///  
    /// Ngừng sử dụng một cấu hình tỷ lệ.
    ///
    /// Hệ thống không xóa bản ghi khỏi database.
    /// Service chỉ cập nhật:
    /// - IsActive = false.
    /// - EffectiveTo = ngày kết thúc.
    ///  
    Task<BhxhRateConfigDto> DeactivateRateConfigAsync(
        int rateConfigId,
        DeactivateBhxhRateConfigRequest request,
        int adminUserId);


    // ========================================================
    // 3. HỒ SƠ BHXH NHÂN VIÊN
    // ========================================================

    ///  
    /// Lấy toàn bộ hồ sơ BHXH để Admin quản lý.
    ///  
    Task<List<BhxhEmployeeProfileDto>>
        GetAllProfilesAsync();

    ///  
    /// Lấy một hồ sơ theo ID hồ sơ.
    /// Dùng cho màn hình xem chi tiết của Admin.
    ///  
    Task<BhxhEmployeeProfileDto> GetProfileByIdAsync(
        int profileId);

    ///  
    /// Lấy hồ sơ BHXH theo ID nhân viên.
    ///
    /// Staff có thể dùng chức năng này để xem
    /// hồ sơ của chính mình.
    ///  
    Task<BhxhEmployeeProfileDto> GetProfileByUserIdAsync(
        int userId);

    ///  
    /// Tạo hồ sơ BHXH mới cho nhân viên.
    ///
    /// Service sẽ kiểm tra:
    /// - Nhân viên phải tồn tại.
    /// - Nhân viên phải là FULL_TIME.
    /// - Nhân viên chưa có hồ sơ.
    /// - Mã số BHXH không bị trùng.
    /// - Mức lương làm căn cứ phải lớn hơn 0.
    /// - Ngày kết thúc không trước ngày bắt đầu.
    ///
    /// Hồ sơ mới được tạo với trạng thái PENDING.
    ///  
    Task<BhxhEmployeeProfileDto> CreateProfileAsync(
        CreateBhxhEmployeeProfileRequest request,
        int adminUserId);

    ///  
    /// Cập nhật thông tin hồ sơ BHXH.
    ///
    /// Không cho phép đổi UserId của hồ sơ.
    ///  
    Task<BhxhEmployeeProfileDto> UpdateProfileAsync(
        int profileId,
        UpdateBhxhEmployeeProfileRequest request,
        int adminUserId);

    ///  
    /// Chuyển trạng thái hồ sơ:
    /// - PENDING
    /// - ACTIVE
    /// - SUSPENDED
    /// - STOPPED
    ///
    /// Đây là cơ chế ngừng hồ sơ theo nghiệp vụ,
    /// không xóa cứng dữ liệu.
    ///  
    Task<BhxhEmployeeProfileDto> UpdateProfileStatusAsync(
        int profileId,
        UpdateBhxhProfileStatusRequest request,
        int adminUserId);


    // ========================================================
    // 4. KHOẢN ĐÓNG BHXH HẰNG THÁNG
    // ========================================================

    ///  
    /// Sinh khoản đóng BHXH cho các hồ sơ ACTIVE
    /// trong tháng và năm được chọn.
    ///
    /// Service sẽ:
    /// - Tìm tỷ lệ đang có hiệu lực.
    /// - Lấy các hồ sơ đủ điều kiện.
    /// - Không tạo trùng khoản đóng.
    /// - Tính tiền nhân viên đóng.
    /// - Tính tiền doanh nghiệp đóng.
    /// - Tạo bản ghi với trạng thái DRAFT.
    ///  
    Task<GenerateBhxhContributionsResultDto>
        GenerateContributionsAsync(
            GenerateBhxhContributionsRequest request);

    ///  
    /// Lấy danh sách khoản đóng của một tháng và năm.
    /// Dùng cho màn hình quản lý của Admin.
    ///  
    Task<List<BhxhMonthlyContributionDto>>
        GetContributionsByPeriodAsync(
            int month,
            int year);

    ///  
    /// Lấy toàn bộ lịch sử đóng BHXH của một nhân viên.
    ///
    /// Staff sẽ truyền ID của chính mình.
    ///  
    Task<List<BhxhMonthlyContributionDto>>
        GetContributionsByUserIdAsync(int userId);

    ///  
    /// Lấy chi tiết một khoản đóng theo ID.
    ///  
    Task<BhxhMonthlyContributionDto>
        GetContributionByIdAsync(int contributionId);

    ///  
    /// Xác nhận khoản đóng:
    ///
    /// DRAFT → CONFIRMED.
    ///
    /// Không thể xác nhận khoản đóng đã PAID hoặc CANCELLED.
    ///  
    Task<BhxhMonthlyContributionDto>
        ConfirmContributionAsync(
            int contributionId,
            int adminUserId);

    ///  
    /// Đánh dấu khoản đóng đã được nộp:
    ///
    /// CONFIRMED → PAID.
    ///
    /// Chỉ khoản đóng đã xác nhận mới được chuyển sang PAID.
    ///  
    Task<BhxhMonthlyContributionDto>
        MarkContributionPaidAsync(
            int contributionId,
            int adminUserId);

    ///  
    /// Hủy một khoản đóng bị tạo sai.
    ///
    /// Hệ thống không xóa khỏi database.
    /// Trạng thái được chuyển sang CANCELLED
    /// và giữ lại lý do hủy trong Note.
    ///  
    Task<BhxhMonthlyContributionDto>
        CancelContributionAsync(
            int contributionId,
            CancelBhxhContributionRequest request,
            int adminUserId);
}
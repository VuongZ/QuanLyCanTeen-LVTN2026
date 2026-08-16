using LuanVanTotNghiep.DTOs;

namespace LuanVanTotNghiep.Services;

// Khai báo các chức năng nghiệp vụ
// của phân hệ bảo hiểm xã hội.
//
// Interface chỉ mô tả Service có thể làm gì.
// Việc kiểm tra và xử lý nghiệp vụ thật
// được thực hiện trong SocialInsuranceService.
public interface ISocialInsuranceService
{
    // ========================================================
    // 1. NHÂN VIÊN FULL TIME
    // ========================================================

    // Lấy danh sách nhân viên FULL TIME chưa bị xóa.
    //
    // Kết quả cho biết:
    // - Nhân viên đã có hồ sơ BHXH hay chưa.
    // - Hồ sơ hiện tại có trạng thái gì.
    Task<List<SocialInsuranceFullTimeEmployeeDto>>
        GetFullTimeEmployeesAsync();


    // ========================================================
    // 2. CẤU HÌNH TỶ LỆ ĐÓNG BHXH
    // ========================================================

    // Lấy toàn bộ lịch sử cấu hình tỷ lệ đóng BHXH.
    Task<List<BhxhRateConfigDto>>
        GetAllRateConfigsAsync();

    // Tạo cấu hình tỷ lệ đóng BHXH mới.
    //
    // Service kiểm tra:
    // - Tỷ lệ hợp lệ.
    // - Khoảng thời gian hợp lệ.
    // - Không trùng ngày bắt đầu hiệu lực.
    Task<BhxhRateConfigDto> CreateRateConfigAsync(
        CreateBhxhRateConfigRequest request,
        int adminUserId);

    // Cập nhật cấu hình tỷ lệ đóng BHXH.
    //
    // Chỉ cho phép sửa khi:
    // - Cấu hình vẫn hoạt động.
    // - Chưa đến ngày hiệu lực.
    // - Chưa được dùng để sinh khoản đóng.
    Task<BhxhRateConfigDto> UpdateRateConfigAsync(
        int rateConfigId,
        UpdateBhxhRateConfigRequest request,
        int adminUserId);

    // Ngừng sử dụng một cấu hình tỷ lệ.
    //
    // Hệ thống không xóa bản ghi.
    // Chỉ cập nhật IsActive và EffectiveTo.
    Task<BhxhRateConfigDto> DeactivateRateConfigAsync(
        int rateConfigId,
        DeactivateBhxhRateConfigRequest request,
        int adminUserId);


    // ========================================================
    // 3. HỒ SƠ BHXH NHÂN VIÊN
    // ========================================================

    // Lấy toàn bộ hồ sơ BHXH để Admin quản lý.
    Task<List<BhxhEmployeeProfileDto>>
        GetAllProfilesAsync();

    // Lấy hồ sơ theo ID hồ sơ.
    // Dùng cho màn hình chi tiết của Admin.
    Task<BhxhEmployeeProfileDto> GetProfileByIdAsync(
        int profileId);

    // Lấy hồ sơ theo ID nhân viên.
    //
    // Staff dùng chức năng này để xem
    // hồ sơ BHXH của chính mình.
    Task<BhxhEmployeeProfileDto> GetProfileByUserIdAsync(
        int userId);

    // Tạo hồ sơ BHXH mới cho nhân viên.
    //
    // Quy tắc:
    // - Chỉ nhân viên FULL TIME.
    // - Mỗi nhân viên có tối đa một hồ sơ.
    // - Mã số BHXH không được trùng.
    // - Hồ sơ mới có trạng thái PENDING.
    // - Xác nhận Staff ban đầu là PENDING.
    Task<BhxhEmployeeProfileDto> CreateProfileAsync(
        CreateBhxhEmployeeProfileRequest request,
        int adminUserId);

    // Admin cập nhật hồ sơ BHXH.
    //
    // Khi thay đổi thông tin quan trọng:
    // - Mã số BHXH.
    // - Mức lương làm căn cứ.
    // - Ngày bắt đầu.
    // - Ngày kết thúc.
    //
    // Hồ sơ sẽ trở lại PENDING
    // và Staff phải xác nhận lại.
    Task<BhxhEmployeeProfileDto> UpdateProfileAsync(
        int profileId,
        UpdateBhxhEmployeeProfileRequest request,
        int adminUserId);

    // Admin chuyển trạng thái hồ sơ:
    // - PENDING
    // - ACTIVE
    // - SUSPENDED
    // - STOPPED
    //
    // Chỉ được chuyển sang ACTIVE
    // khi Staff đã xác nhận CONFIRMED.
    Task<BhxhEmployeeProfileDto> UpdateProfileStatusAsync(
        int profileId,
        UpdateBhxhProfileStatusRequest request,
        int adminUserId);

    // Staff kiểm tra hồ sơ BHXH của chính mình.
    //
    // Staff chỉ được chọn:
    // - CONFIRMED
    // - CHANGE_REQUESTED
    //
    // CHANGE_REQUESTED bắt buộc phải có nội dung phản hồi.
    Task<BhxhEmployeeProfileDto>
        UpdateMyProfileConfirmationAsync(
            int staffUserId,
            UpdateMyBhxhConfirmationRequest request);


    // ========================================================
    // 4. KHOẢN ĐÓNG BHXH HẰNG THÁNG
    // ========================================================

    // Sinh khoản đóng cho các hồ sơ đủ điều kiện:
    // - Hồ sơ ACTIVE.
    // - Staff đã CONFIRMED.
    // - Nhân viên vẫn là FULL TIME.
    // - Thời gian hồ sơ giao với tháng được chọn.
    //
    // Khoản đóng mới có trạng thái DRAFT.
    Task<GenerateBhxhContributionsResultDto>
        GenerateContributionsAsync(
            GenerateBhxhContributionsRequest request);

    // Lấy danh sách khoản đóng theo tháng và năm.
    Task<List<BhxhMonthlyContributionDto>>
        GetContributionsByPeriodAsync(
            int month,
            int year);

    // Lấy lịch sử đóng BHXH của một nhân viên.
    Task<List<BhxhMonthlyContributionDto>>
        GetContributionsByUserIdAsync(
            int userId);

    // Lấy chi tiết một khoản đóng.
    Task<BhxhMonthlyContributionDto>
        GetContributionByIdAsync(
            int contributionId);

    // Xác nhận khoản đóng:
    // DRAFT → CONFIRMED.
    Task<BhxhMonthlyContributionDto>
        ConfirmContributionAsync(
            int contributionId,
            int adminUserId);

    // Đánh dấu khoản đóng đã nộp:
    // CONFIRMED → PAID.
    Task<BhxhMonthlyContributionDto>
        MarkContributionPaidAsync(
            int contributionId,
            int adminUserId);

    // Hủy khoản đóng được tạo sai.
    //
    // Không xóa bản ghi.
    // Trạng thái chuyển thành CANCELLED.
    Task<BhxhMonthlyContributionDto>
        CancelContributionAsync(
            int contributionId,
            CancelBhxhContributionRequest request,
            int adminUserId);
}
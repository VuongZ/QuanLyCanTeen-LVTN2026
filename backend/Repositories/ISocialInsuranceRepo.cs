using LuanVanTotNghiep.backend.Models.Entities;

namespace LuanVanTotNghiep.Repositories;

///  
/// Khai báo các thao tác truy cập database
/// phục vụ chức năng bảo hiểm xã hội.
///
/// Repository chỉ chịu trách nhiệm truy vấn và lưu dữ liệu.
/// Các quy tắc nghiệp vụ như:
/// - Chỉ FULL TIME được tham gia.
/// - Hồ sơ phải ACTIVE.
/// - Công thức tính tiền đóng.
///
/// sẽ được xử lý trong SocialInsuranceService.
///  
public interface ISocialInsuranceRepo
{
    // ========================================================
    // NHÂN VIÊN
    // ========================================================

    ///  
    /// Lấy danh sách nhân viên FULL TIME chưa bị xóa.
    ///  
    Task<List<NsUser>> GetFullTimeEmployeesAsync();

    ///  
    /// Tìm nhân viên theo ID.
    /// Dùng khi kiểm tra nhân viên có tồn tại
    /// và có phải FULL TIME hay không.
    ///  
    Task<NsUser?> GetUserByIdAsync(int userId);


    // ========================================================
// CẤU HÌNH TỶ LỆ BHXH
// ========================================================


/// Lấy toàn bộ lịch sử cấu hình tỷ lệ,
/// sắp xếp từ cấu hình mới nhất đến cũ nhất.

Task<List<BhxhRateConfig>>
    GetAllRateConfigsAsync();



/// Tìm một cấu hình tỷ lệ theo ID.
///
/// Phương thức này dùng để đọc dữ liệu,
/// không dùng để cập nhật.

Task<BhxhRateConfig?>
    GetRateConfigByIdAsync(
        int id);



/// Lấy cấu hình tỷ lệ theo ID để cập nhật.
///
/// Không dùng AsNoTracking vì Service có thể thay đổi:
/// - EmployeeRate.
/// - EmployerRate.
/// - EffectiveFrom.
/// - EffectiveTo.
/// - IsActive.
///
/// Sau đó gọi SaveChangesAsync để lưu xuống database.

Task<BhxhRateConfig?>
    GetRateConfigByIdForUpdateAsync(
        int id);



/// Tìm cấu hình có hiệu lực tại một ngày cụ thể.
///
/// Ví dụ cần tính BHXH tháng 8/2026 thì Service
/// sẽ truyền ngày 01/08/2026.

Task<BhxhRateConfig?>
    GetEffectiveRateConfigAsync(
        DateOnly targetDate);



/// Kiểm tra đã có cấu hình bắt đầu
/// vào ngày này hay chưa.

Task<bool>
    RateConfigExistsByEffectiveFromAsync(
        DateOnly effectiveFrom);



/// Kiểm tra cấu hình tỷ lệ đã từng được dùng
/// để sinh khoản đóng BHXH hay chưa.
///
/// true:
/// Có ít nhất một khoản đóng sử dụng cấu hình.
///
/// false:
/// Chưa có khoản đóng nào sử dụng cấu hình.

Task<bool>
    RateConfigHasContributionsAsync(
        int rateConfigId);



/// Thêm cấu hình tỷ lệ mới vào DbContext.
///
/// Chưa lưu database cho đến khi
/// gọi SaveChangesAsync.

Task AddRateConfigAsync(
    BhxhRateConfig rateConfig);


    // ========================================================
    // HỒ SƠ BHXH
    // ========================================================

    ///  
    /// Lấy tất cả hồ sơ BHXH để Admin quản lý.
    ///  
    Task<List<BhxhEmployeeProfile>> GetAllProfilesAsync();

    ///  
    /// Lấy hồ sơ theo ID để hiển thị.
    /// Kết quả không được EF theo dõi thay đổi.
    ///  
    Task<BhxhEmployeeProfile?> GetProfileByIdAsync(
        int profileId);

    ///  
    /// Lấy hồ sơ theo ID để cập nhật.
    /// Kết quả được EF theo dõi thay đổi.
    ///  
    Task<BhxhEmployeeProfile?> GetProfileByIdForUpdateAsync(
        int profileId);

    ///  
    /// Tìm hồ sơ BHXH theo nhân viên.
    ///  
    Task<BhxhEmployeeProfile?> GetProfileByUserIdAsync(
        int userId);

        // Lấy hồ sơ của một nhân viên để cập nhật.
//
// Hàm này không dùng AsNoTracking ở Repository,
// vì Staff sẽ thay đổi trạng thái xác nhận hồ sơ.
Task<BhxhEmployeeProfile?>
    GetProfileByUserIdForUpdateAsync(
        int userId);

    ///  
    /// Kiểm tra một nhân viên đã có hồ sơ BHXH chưa.
    ///  
    Task<bool> ProfileExistsForUserAsync(int userId);

    ///  
    /// Kiểm tra mã số BHXH đã được sử dụng chưa.
    ///
    /// excludeProfileId được dùng khi cập nhật:
    /// hồ sơ hiện tại được phép giữ nguyên mã số của chính nó.
    ///  
    Task<bool> SocialInsuranceNumberExistsAsync(
        string socialInsuranceNumber,
        int? excludeProfileId = null);

    ///  
    /// Thêm hồ sơ mới vào DbContext.
    ///  
    Task AddProfileAsync(BhxhEmployeeProfile profile);

    ///  
    /// Lấy các hồ sơ ACTIVE có thời gian tham gia
    /// giao với tháng cần tính BHXH.
    ///  
    Task<List<BhxhEmployeeProfile>>
        GetActiveProfilesForPeriodAsync(
            int month,
            int year);


    // ========================================================
    // KHOẢN ĐÓNG BHXH HẰNG THÁNG
    // ========================================================

    ///  
    /// Lấy danh sách khoản đóng theo tháng và năm.
    ///  
    Task<List<BhxhMonthlyContribution>>
        GetContributionsByPeriodAsync(
            int month,
            int year);

    ///  
    /// Lấy lịch sử đóng BHXH của một nhân viên.
    ///  
    Task<List<BhxhMonthlyContribution>>
        GetContributionsByUserIdAsync(int userId);

    ///  
    /// Lấy một khoản đóng theo ID để hiển thị.
    ///  
    Task<BhxhMonthlyContribution?>
        GetContributionByIdAsync(int contributionId);

    ///  
    /// Lấy một khoản đóng theo ID để cập nhật trạng thái.
    ///  
    Task<BhxhMonthlyContribution?>
        GetContributionByIdForUpdateAsync(
            int contributionId);

    ///  
    /// Kiểm tra nhân viên đã có khoản đóng
    /// trong tháng và năm này chưa.
    ///  
    Task<bool> ContributionExistsAsync(
        int userId,
        int month,
        int year);

    ///  
    /// Thêm nhiều khoản đóng cùng lúc.
    ///
    /// Dùng khi Admin sinh khoản đóng cho toàn bộ
    /// hồ sơ ACTIVE trong một tháng.
    ///  
    Task AddContributionsAsync(
        IEnumerable<BhxhMonthlyContribution> contributions);


    // ========================================================
    // LƯU THAY ĐỔI
    // ========================================================

    ///  
    /// Lưu toàn bộ thay đổi đang được EF theo dõi
    /// xuống database.
    ///  
    Task<int> SaveChangesAsync();
}
using LuanVanTotNghiep.backend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Repositories;

///  
/// Repository truy cập các bảng BHXH.
///
/// Repository này không kế thừa Repository&lt;T&gt;
/// vì nó phải làm việc với nhiều Entity:
/// - NsUser
/// - BhxhRateConfig
/// - BhxhEmployeeProfile
/// - BhxhMonthlyContribution
///
/// Repository chỉ truy vấn và lưu dữ liệu.
/// Quy tắc nghiệp vụ sẽ đặt trong Service.
///  
public class SocialInsuranceRepo : ISocialInsuranceRepo
{
    private readonly AppDbContext _context;

    ///  
    /// AppDbContext được Dependency Injection truyền vào.
    ///  
    public SocialInsuranceRepo(AppDbContext context)
    {
        _context = context;
    }


    // ========================================================
    // NHÂN VIÊN
    // ========================================================

    public async Task<List<NsUser>>
    GetFullTimeEmployeesAsync()
{
    return await _context.NsUsers

        // Đây là truy vấn chỉ đọc nên không cần
        // Entity Framework theo dõi thay đổi.
        .AsNoTracking()

        // Theo nghiệp vụ của nhóm:
        // chỉ nhân viên FULL TIME tham gia BHXH.
        .Where(user =>
            user.IsDeleted != true &&
            user.EmploymentType == "FULL TIME")

        // Sắp xếp theo tên để Admin dễ tìm.
        .OrderBy(user =>
            user.FullName)

        .ToListAsync();
}

    public async Task<NsUser?> GetUserByIdAsync(int userId)
    {
        return await _context.NsUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(user =>
                user.Id == userId &&
                user.IsDeleted != true);
    }


    // ========================================================
    // CẤU HÌNH TỶ LỆ BHXH
    // ========================================================

    public async Task<List<BhxhRateConfig>>
        GetAllRateConfigsAsync()
    {
        return await _context.BhxhRateConfigs
            .AsNoTracking()

            // Nạp Admin đã tạo cấu hình để trả tên ra DTO.
            .Include(rate => rate.CreatedByUser)

            // Cấu hình mới nhất hiển thị trước.
            .OrderByDescending(rate => rate.EffectiveFrom)

            .ToListAsync();
    }

    public async Task<BhxhRateConfig?>
        GetRateConfigByIdAsync(int id)
    {
        return await _context.BhxhRateConfigs
            .AsNoTracking()
            .Include(rate => rate.CreatedByUser)
            .FirstOrDefaultAsync(rate => rate.Id == id);
    }

    ///  
/// Lấy cấu hình tỷ lệ để cập nhật trạng thái.
///
/// Không dùng AsNoTracking vì Entity Framework cần theo dõi
/// Entity này. Khi Service thay đổi IsActive hoặc EffectiveTo,
/// SaveChangesAsync sẽ tự tạo câu lệnh UPDATE.
///  
public async Task<BhxhRateConfig?>
    GetRateConfigByIdForUpdateAsync(int id)
{
    return await _context.BhxhRateConfigs

        // Không có AsNoTracking tại đây.
        // Entity trả về sẽ được EF theo dõi thay đổi.
        .FirstOrDefaultAsync(rate => rate.Id == id);
}

    public async Task<BhxhRateConfig?>
    GetEffectiveRateConfigAsync(DateOnly targetDate)
{
    return await _context.BhxhRateConfigs
        .AsNoTracking()

        // Không lọc IsActive tại đây.
        //
        // Lý do:
        // Một cấu hình cũ dù đã ngừng sử dụng vẫn phải
        // được tìm thấy khi xem hoặc tính lại dữ liệu
        // thuộc thời gian mà cấu hình đó từng có hiệu lực.
        //
        // Ví dụ:
        // - Cấu hình A hiệu lực đến 31/08/2026.
        // - Sau đó A được chuyển IsActive = false.
        // - Khi tra tháng 08/2026, hệ thống vẫn phải lấy A.
        .Where(rate =>
            rate.EffectiveFrom <= targetDate)

        .Where(rate =>
            rate.EffectiveTo == null ||
            rate.EffectiveTo >= targetDate)

        // Trong trường hợp dữ liệu bị chồng thời gian,
        // ưu tiên cấu hình có ngày bắt đầu gần nhất.
        .OrderByDescending(rate =>
            rate.EffectiveFrom)

        .FirstOrDefaultAsync();
}

    public async Task<bool>
        RateConfigExistsByEffectiveFromAsync(
            DateOnly effectiveFrom)
    {
        return await _context.BhxhRateConfigs
            .AnyAsync(rate =>
                rate.EffectiveFrom == effectiveFrom);
    }

    public async Task AddRateConfigAsync(
        BhxhRateConfig rateConfig)
    {
        // AddAsync chỉ đưa Entity vào trạng thái Added.
        // Chưa ghi xuống database ở dòng này.
        await _context.BhxhRateConfigs.AddAsync(rateConfig);
    }

    /// <summary>
/// Kiểm tra bảng bhxh_monthly_contribution
/// có khoản đóng nào sử dụng cấu hình này hay chưa.
///
/// Chỉ cần tồn tại một bản ghi là cấu hình
/// không được phép sửa trực tiếp nữa.
/// </summary>
public async Task<bool>
    RateConfigHasContributionsAsync(
        int rateConfigId)
{
    return await _context
        .BhxhMonthlyContributions
        .AsNoTracking()
        .AnyAsync(contribution =>
            contribution.RateConfigId ==
            rateConfigId);
}


    // ========================================================
    // HỒ SƠ BHXH
    // ========================================================

    public async Task<List<BhxhEmployeeProfile>>
        GetAllProfilesAsync()
    {
        return await _context.BhxhEmployeeProfiles
            .AsNoTracking()

            // Nạp nhân viên sở hữu hồ sơ.
            .Include(profile => profile.User)

            // Nạp Admin tạo hồ sơ.
            .Include(profile => profile.CreatedByUser)

            // Nạp Admin cập nhật hồ sơ gần nhất.
            .Include(profile => profile.UpdatedByUser)

            // Hồ sơ mới tạo hiển thị trước.
            .OrderByDescending(profile => profile.CreatedAt)

            .ToListAsync();
    }

    public async Task<BhxhEmployeeProfile?>
        GetProfileByIdAsync(int profileId)
    {
        return await _context.BhxhEmployeeProfiles
            .AsNoTracking()
            .Include(profile => profile.User)
            .Include(profile => profile.CreatedByUser)
            .Include(profile => profile.UpdatedByUser)
            .FirstOrDefaultAsync(profile =>
                profile.Id == profileId);
    }

    // Lấy hồ sơ của Staff để cập nhật trạng thái xác nhận.
//
// Không dùng AsNoTracking vì Entity Framework phải theo dõi
// Entity này để SaveChangesAsync có thể lưu thay đổi.
public async Task<BhxhEmployeeProfile?>
    GetProfileByUserIdForUpdateAsync(
        int userId)
{
    return await _context.BhxhEmployeeProfiles

        // Nạp thông tin nhân viên để Service kiểm tra
        // người này vẫn là nhân viên FULL TIME.
        .Include(profile => profile.User)

        // Nạp thông tin Admin tạo và cập nhật hồ sơ,
        // để sau khi lưu có thể trả DTO đầy đủ.
        .Include(profile => profile.CreatedByUser)
        .Include(profile => profile.UpdatedByUser)

        .FirstOrDefaultAsync(profile =>
            profile.UserId == userId);
}

    public async Task<BhxhEmployeeProfile?>
        GetProfileByIdForUpdateAsync(int profileId)
    {
        // Không dùng AsNoTracking vì Service sẽ sửa Entity này.
        // Khi gọi SaveChangesAsync, EF sẽ phát hiện thay đổi.
        return await _context.BhxhEmployeeProfiles
            .Include(profile => profile.User)
            .Include(profile => profile.CreatedByUser)
            .Include(profile => profile.UpdatedByUser)
            .FirstOrDefaultAsync(profile =>
                profile.Id == profileId);
    }

    public async Task<BhxhEmployeeProfile?>
        GetProfileByUserIdAsync(int userId)
    {
        return await _context.BhxhEmployeeProfiles
            .AsNoTracking()
            .Include(profile => profile.User)
            .Include(profile => profile.CreatedByUser)
            .Include(profile => profile.UpdatedByUser)
            .FirstOrDefaultAsync(profile =>
                profile.UserId == userId);
    }

    public async Task<bool>
        ProfileExistsForUserAsync(int userId)
    {
        return await _context.BhxhEmployeeProfiles
            .AnyAsync(profile =>
                profile.UserId == userId);
    }

    public async Task<bool>
        SocialInsuranceNumberExistsAsync(
            string socialInsuranceNumber,
            int? excludeProfileId = null)
    {
        // Chuẩn hóa dữ liệu để tránh khoảng trắng đầu/cuối.
        string normalizedNumber =
            socialInsuranceNumber.Trim();

        return await _context.BhxhEmployeeProfiles
            .AnyAsync(profile =>

                // So sánh mã số BHXH.
                profile.SocialInsuranceNumber ==
                    normalizedNumber &&

                // Khi cập nhật, bỏ qua chính hồ sơ đang sửa.
                (
                    !excludeProfileId.HasValue ||
                    profile.Id != excludeProfileId.Value
                ));
    }

    public async Task AddProfileAsync(
        BhxhEmployeeProfile profile)
    {
        await _context.BhxhEmployeeProfiles
            .AddAsync(profile);
    }

    public async Task<List<BhxhEmployeeProfile>>
    GetActiveProfilesForPeriodAsync(
        int month,
        int year)
{
    // Ngày đầu tiên của tháng cần tính.
    DateOnly firstDayOfMonth =
        new DateOnly(year, month, 1);

    // Ngày cuối cùng của tháng cần tính.
    DateOnly lastDayOfMonth =
        firstDayOfMonth
            .AddMonths(1)
            .AddDays(-1);

    return await _context.BhxhEmployeeProfiles

        // Nạp thông tin nhân viên để Service
        // tạo khoản đóng và kiểm tra FULL TIME.
        .Include(profile => profile.User)

        .Where(profile =>

            // Hồ sơ phải được Admin kích hoạt.
            profile.Status == "ACTIVE" &&

            // Staff phải xác nhận thông tin hồ sơ.
            profile.StaffConfirmationStatus ==
                "CONFIRMED" &&

            // Chỉ FULL TIME tham gia BHXH.
            profile.User.EmploymentType ==
                "FULL TIME" &&

            // Không tính cho nhân viên đã bị xóa.
            profile.User.IsDeleted != true &&

            // Hồ sơ phải bắt đầu trước hoặc
            // trong tháng được chọn.
            profile.StartDate <= lastDayOfMonth &&

            // EndDate NULL nghĩa là vẫn tham gia.
            // Nếu có EndDate thì thời gian tham gia
            // phải giao với tháng đang tính.
            (
                profile.EndDate == null ||
                profile.EndDate >= firstDayOfMonth
            ))

        .OrderBy(profile =>
            profile.User.FullName)

        .ToListAsync();
}


    // ========================================================
    // KHOẢN ĐÓNG BHXH HẰNG THÁNG
    // ========================================================

    public async Task<List<BhxhMonthlyContribution>>
        GetContributionsByPeriodAsync(
            int month,
            int year)
    {
        return await _context.BhxhMonthlyContributions
            .AsNoTracking()

            // Nạp nhân viên phát sinh khoản đóng.
            .Include(contribution => contribution.User)

            // Nạp hồ sơ BHXH được sử dụng.
            .Include(contribution => contribution.Profile)

            // Nạp cấu hình tỷ lệ đã dùng.
            .Include(contribution => contribution.RateConfig)

            // Nạp Admin đã xác nhận.
            .Include(contribution =>
                contribution.ConfirmedByUser)

            // Nạp Admin đánh dấu đã đóng.
            .Include(contribution =>
                contribution.PaidByUser)

            .Where(contribution =>
                contribution.Month == month &&
                contribution.Year == year)

            .OrderBy(contribution =>
                contribution.User.FullName)

            .ToListAsync();
    }

    public async Task<List<BhxhMonthlyContribution>>
        GetContributionsByUserIdAsync(int userId)
    {
        return await _context.BhxhMonthlyContributions
            .AsNoTracking()
            .Include(contribution => contribution.User)
            .Include(contribution => contribution.Profile)
            .Include(contribution => contribution.RateConfig)
            .Include(contribution =>
                contribution.ConfirmedByUser)
            .Include(contribution =>
                contribution.PaidByUser)

            .Where(contribution =>
                contribution.UserId == userId)

            // Năm gần nhất hiển thị trước.
            .OrderByDescending(contribution =>
                contribution.Year)

            // Trong cùng năm, tháng gần nhất hiển thị trước.
            .ThenByDescending(contribution =>
                contribution.Month)

            .ToListAsync();
    }

    public async Task<BhxhMonthlyContribution?>
        GetContributionByIdAsync(int contributionId)
    {
        return await _context.BhxhMonthlyContributions
            .AsNoTracking()
            .Include(contribution => contribution.User)
            .Include(contribution => contribution.Profile)
            .Include(contribution => contribution.RateConfig)
            .Include(contribution =>
                contribution.ConfirmedByUser)
            .Include(contribution =>
                contribution.PaidByUser)
            .Include(contribution =>
                contribution.MonthlySalary)

            .FirstOrDefaultAsync(contribution =>
                contribution.Id == contributionId);
    }

    public async Task<BhxhMonthlyContribution?>
        GetContributionByIdForUpdateAsync(
            int contributionId)
    {
        // Không dùng AsNoTracking vì Service sẽ đổi trạng thái
        // DRAFT -> CONFIRMED hoặc CONFIRMED -> PAID.
        return await _context.BhxhMonthlyContributions
            .Include(contribution => contribution.User)
            .Include(contribution => contribution.Profile)
            .Include(contribution => contribution.RateConfig)
            .Include(contribution =>
                contribution.ConfirmedByUser)
            .Include(contribution =>
                contribution.PaidByUser)
            .Include(contribution =>
                contribution.MonthlySalary)

            .FirstOrDefaultAsync(contribution =>
                contribution.Id == contributionId);
    }

    public async Task<bool> ContributionExistsAsync(
        int userId,
        int month,
        int year)
    {
        return await _context.BhxhMonthlyContributions
            .AnyAsync(contribution =>
                contribution.UserId == userId &&
                contribution.Month == month &&
                contribution.Year == year);
    }

    public async Task AddContributionsAsync(
        IEnumerable<BhxhMonthlyContribution> contributions)
    {
        // AddRangeAsync thêm nhiều Entity cùng lúc,
        // hiệu quả hơn việc SaveChanges từng bản ghi.
        await _context.BhxhMonthlyContributions
            .AddRangeAsync(contributions);
    }


    // ========================================================
    // LƯU THAY ĐỔI
    // ========================================================

    public async Task<int> SaveChangesAsync()
    {
        // Giá trị trả về là số dòng dữ liệu
        // bị ảnh hưởng trong database.
        return await _context.SaveChangesAsync();
    }
}

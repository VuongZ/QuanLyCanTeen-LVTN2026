using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Repositories;

namespace LuanVanTotNghiep.Services;

///  
/// Xử lý toàn bộ nghiệp vụ BHXH.
///
/// Service chịu trách nhiệm:
/// - Kiểm tra dữ liệu.
/// - Kiểm tra nhân viên FULL_TIME.
/// - Tính tiền đóng BHXH.
/// - Chuyển trạng thái.
/// - Không xóa cứng dữ liệu.
///
/// Repository chỉ chịu trách nhiệm đọc và lưu database.
///  
public class SocialInsuranceService
    : ISocialInsuranceService
{
    private const string FullTimeType = "FULL_TIME";

    private const string PendingStatus = "PENDING";
    private const string ActiveStatus = "ACTIVE";
    private const string SuspendedStatus = "SUSPENDED";
    private const string StoppedStatus = "STOPPED";

    private const string DraftStatus = "DRAFT";
    private const string ConfirmedStatus = "CONFIRMED";
    private const string PaidStatus = "PAID";
    private const string CancelledStatus = "CANCELLED";

    private readonly ISocialInsuranceRepo _repo;

    ///  
    /// Repository được Dependency Injection truyền vào.
    ///  
    public SocialInsuranceService(
        ISocialInsuranceRepo repo)
    {
        _repo = repo;
    }


    // ========================================================
    // HÀM HỖ TRỢ THỜI GIAN VIỆT NAM
    // ========================================================

    ///  
    /// Tìm múi giờ Việt Nam trên Windows hoặc Linux.
    ///  
    private static TimeZoneInfo GetVietnamTimeZone()
    {
        try
        {
            // ID thường dùng trên Windows.
            return TimeZoneInfo.FindSystemTimeZoneById(
                "SE Asia Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            // ID thường dùng trên Linux.
            return TimeZoneInfo.FindSystemTimeZoneById(
                "Asia/Ho_Chi_Minh");
        }
    }

    private static DateTime GetVietnamNow()
    {
        return TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            GetVietnamTimeZone());
    }

    private static DateOnly GetVietnamToday()
    {
        return DateOnly.FromDateTime(
            GetVietnamNow());
    }


    // ========================================================
    // HÀM KIỂM TRA DỮ LIỆU DÙNG CHUNG
    // ========================================================

    ///  
    /// Chuẩn hóa chuỗi:
    /// - Xóa khoảng trắng đầu và cuối.
    /// - Chuỗi rỗng được chuyển thành null.
    ///  
    private static string? NormalizeNullableText(
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    ///  
    /// Kiểm tra nhân viên có phải FULL_TIME hay không.
    ///  
    private static void EnsureFullTimeEmployee(
        NsUser user)
    {
        if (!string.Equals(
                user.EmploymentType,
                FullTimeType,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Chỉ nhân viên FULL_TIME mới được tham gia BHXH.");
        }

        if (user.IsDeleted == true)
        {
            throw new InvalidOperationException(
                "Nhân viên đã bị ngừng sử dụng trong hệ thống.");
        }
    }

    ///  
    /// Kiểm tra người thực hiện thao tác có tồn tại.
    ///
    /// Việc kiểm tra đúng vai trò Admin sẽ được bảo vệ thêm
    /// tại Controller bằng Authorize.
    ///  
    private async Task EnsureActorExistsAsync(
        int userId)
    {
        if (userId <= 0)
        {
            throw new ArgumentException(
                "Người thực hiện thao tác không hợp lệ.");
        }

        var user =
            await _repo.GetUserByIdAsync(userId);

        if (user == null)
        {
            throw new KeyNotFoundException(
                "Không tìm thấy người thực hiện thao tác.");
        }
    }

    ///  
    /// Kiểm tra ngày kết thúc không đứng trước ngày bắt đầu.
    ///  
    private static void ValidateDateRange(
        DateOnly startDate,
        DateOnly? endDate)
    {
        if (endDate.HasValue &&
            endDate.Value < startDate)
        {
            throw new ArgumentException(
                "Ngày kết thúc không được trước ngày bắt đầu.");
        }
    }

    ///  
    /// Kiểm tra hai khoảng thời gian có chồng nhau không.
    ///
    /// EndDate null được hiểu là chưa có ngày kết thúc.
    ///  
    private static bool DateRangesOverlap(
        DateOnly firstStart,
        DateOnly? firstEnd,
        DateOnly secondStart,
        DateOnly? secondEnd)
    {
        var firstEndValue =
            firstEnd ?? DateOnly.MaxValue;

        var secondEndValue =
            secondEnd ?? DateOnly.MaxValue;

        return firstStart <= secondEndValue &&
               secondStart <= firstEndValue;
    }

    ///  
    /// Kiểm tra tháng và năm hợp lệ.
    ///  
    private static void ValidatePeriod(
        int month,
        int year)
    {
        if (month < 1 || month > 12)
        {
            throw new ArgumentException(
                "Tháng phải nằm trong khoảng từ 1 đến 12.");
        }

        if (year < 2000 || year > 2100)
        {
            throw new ArgumentException(
                "Năm không hợp lệ.");
        }
    }


    // ========================================================
    // HÀM CHUYỂN ENTITY SANG DTO
    // ========================================================


/// Chuyển Entity cấu hình tỷ lệ thành DTO.
///
/// hasBeenUsed:
/// - true: cấu hình đã được dùng để sinh khoản đóng.
/// - false: cấu hình chưa được sử dụng.

private static BhxhRateConfigDto MapRateConfig(
    BhxhRateConfig entity,
    bool hasBeenUsed = false)
{
    var today =
        GetVietnamToday();

    /*
      Chỉ cho phép chỉnh sửa khi đồng thời:

      1. Cấu hình vẫn đang hoạt động.
      2. Ngày hiệu lực nằm trong tương lai.
      3. Chưa từng được dùng để sinh khoản đóng.
    */
    var canEdit =
        entity.IsActive &&
        entity.EffectiveFrom > today &&
        !hasBeenUsed;

    return new BhxhRateConfigDto
    {
        Id =
            entity.Id,

        EmployeeRate =
            entity.EmployeeRate,

        EmployerRate =
            entity.EmployerRate,

        EffectiveFrom =
            entity.EffectiveFrom,

        EffectiveTo =
            entity.EffectiveTo,

        IsActive =
            entity.IsActive,

        // Hai trường mới phục vụ Frontend.
        HasBeenUsed =
            hasBeenUsed,

        CanEdit =
            canEdit,

        CreatedByUserId =
            entity.CreatedByUserId,

        CreatedByUserName =
            entity.CreatedByUser?.FullName,

        CreatedAt =
            entity.CreatedAt,

        UpdatedAt =
            entity.UpdatedAt
    };
}

    private static BhxhEmployeeProfileDto MapProfile(
        BhxhEmployeeProfile entity)
    {
        return new BhxhEmployeeProfileDto
        {
            Id = entity.Id,
            UserId = entity.UserId,
            FullName =
                entity.User?.FullName
                ?? string.Empty,
            Email =
                entity.User?.Email
                ?? string.Empty,
            EmploymentType =
                entity.User?.EmploymentType
                ?? string.Empty,
            SocialInsuranceNumber =
                entity.SocialInsuranceNumber,
            InsuranceSalaryBasis =
                entity.InsuranceSalaryBasis,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            Status = entity.Status,
            Note = entity.Note,
            CreatedByUserId =
                entity.CreatedByUserId,
            CreatedByUserName =
                entity.CreatedByUser?.FullName,
            UpdatedByUserId =
                entity.UpdatedByUserId,
            UpdatedByUserName =
                entity.UpdatedByUser?.FullName,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private static BhxhMonthlyContributionDto
        MapContribution(
            BhxhMonthlyContribution entity)
    {
        return new BhxhMonthlyContributionDto
        {
            Id = entity.Id,
            UserId = entity.UserId,
            FullName =
                entity.User?.FullName
                ?? string.Empty,
            ProfileId = entity.ProfileId,
            RateConfigId =
                entity.RateConfigId,
            Month = entity.Month,
            Year = entity.Year,
            InsuranceSalaryBasis =
                entity.InsuranceSalaryBasis,
            EmployeeRate =
                entity.EmployeeRate,
            EmployerRate =
                entity.EmployerRate,
            EmployeeAmount =
                entity.EmployeeAmount,
            EmployerAmount =
                entity.EmployerAmount,
            TotalAmount =
                entity.TotalAmount,
            Status = entity.Status,
            ConfirmedByUserId =
                entity.ConfirmedByUserId,
            ConfirmedByUserName =
                entity.ConfirmedByUser?.FullName,
            ConfirmedAt =
                entity.ConfirmedAt,
            PaidByUserId =
                entity.PaidByUserId,
            PaidByUserName =
                entity.PaidByUser?.FullName,
            PaidAt = entity.PaidAt,
            Note = entity.Note,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }


    // ========================================================
    // 1. NHÂN VIÊN FULL_TIME
    // ========================================================

    public async Task<
        List<SocialInsuranceFullTimeEmployeeDto>>
        GetFullTimeEmployeesAsync()
    {
        var employees =
            await _repo.GetFullTimeEmployeesAsync();

        var profiles =
            await _repo.GetAllProfilesAsync();

        // Mỗi nhân viên chỉ có một hồ sơ nên có thể
        // chuyển danh sách hồ sơ thành Dictionary theo UserId.
        var profileByUserId =
            profiles.ToDictionary(
                profile => profile.UserId);

        return employees
            .Select(user =>
            {
                profileByUserId.TryGetValue(
                    user.Id,
                    out var profile);

                return new
                    SocialInsuranceFullTimeEmployeeDto
                {
                    UserId = user.Id,
                    FullName =
                        user.FullName
                        ?? string.Empty,
                    Email =
                        user.Email
                        ?? string.Empty,
                    PhoneNumber =
                        user.PhoneNumber,
                    EmploymentType =
                        user.EmploymentType
                        ?? string.Empty,
                    HireDate =
                        user.HireDate,
                    HasSocialInsuranceProfile =
                        profile != null,
                    ProfileStatus =
                        profile?.Status
                };
            })
            .ToList();
    }


    // ========================================================
    // 2. CẤU HÌNH TỶ LỆ BHXH
    // ========================================================

    /// <summary>
/// Lấy toàn bộ lịch sử cấu hình tỷ lệ.
///
/// Mỗi cấu hình còn được kiểm tra:
/// - Đã được dùng để sinh khoản đóng chưa.
/// - Admin hiện còn được phép sửa không.
/// </summary>
public async Task<List<BhxhRateConfigDto>>
    GetAllRateConfigsAsync()
{
    var entities =
        await _repo
            .GetAllRateConfigsAsync();

    var result =
        new List<BhxhRateConfigDto>();

    foreach (var entity in entities)
    {
        /*
          Kiểm tra bảng bhxh_monthly_contribution
          có bản ghi dùng RateConfigId này không.
        */
        var hasBeenUsed =
            await _repo
                .RateConfigHasContributionsAsync(
                    entity.Id);

        result.Add(
            MapRateConfig(
                entity,
                hasBeenUsed)
        );
    }

    return result;
}

    /// <summary>
/// Tạo cấu hình tỷ lệ BHXH mới.
///
/// Khi tồn tại cấu hình cũ chưa có ngày kết thúc,
/// hệ thống sẽ tự đặt ngày kết thúc của cấu hình cũ
/// bằng ngày trước ngày bắt đầu của cấu hình mới.
/// </summary>
public async Task<BhxhRateConfigDto>
    CreateRateConfigAsync(
        CreateBhxhRateConfigRequest request,
        int adminUserId)
{
    await EnsureActorExistsAsync(
        adminUserId);

    // Kiểm tra tỷ lệ nhân viên.
    if (
        request.EmployeeRate < 0 ||
        request.EmployeeRate > 100
    )
    {
        throw new ArgumentException(
            "Tỷ lệ nhân viên phải nằm " +
            "trong khoảng từ 0 đến 100.");
    }

    // Kiểm tra tỷ lệ doanh nghiệp.
    if (
        request.EmployerRate < 0 ||
        request.EmployerRate > 100
    )
    {
        throw new ArgumentException(
            "Tỷ lệ doanh nghiệp phải nằm " +
            "trong khoảng từ 0 đến 100.");
    }

    // Ngày kết thúc không được trước ngày bắt đầu.
    ValidateDateRange(
        request.EffectiveFrom,
        request.EffectiveTo);

    /*
      Không cho hai cấu hình cùng bắt đầu
      vào một ngày.
    */
    var duplicateStartDate =
        await _repo
            .RateConfigExistsByEffectiveFromAsync(
                request.EffectiveFrom);

    if (duplicateStartDate)
    {
        throw new InvalidOperationException(
            "Đã có cấu hình tỷ lệ bắt đầu " +
            "vào ngày này.");
    }

    var existingConfigs =
        await _repo
            .GetAllRateConfigsAsync();

    /*
      Tìm cấu hình gần nhất trước cấu hình mới
      nhưng chưa có ngày kết thúc.

      Ví dụ:
      Cấu hình cũ: 01/01/2026 → NULL
      Cấu hình mới: 01/09/2026 → NULL
    */
    var previousOpenEndedConfig =
        existingConfigs
            .Where(existing =>
                existing.EffectiveFrom <
                    request.EffectiveFrom &&
                existing.EffectiveTo == null)
            .OrderByDescending(existing =>
                existing.EffectiveFrom)
            .FirstOrDefault();

    /*
      Khi kiểm tra chồng lịch, tạm loại cấu hình
      chưa có ngày kết thúc vừa tìm được.

      Cấu hình đó sẽ được tự động kết thúc
      ở ngày trước cấu hình mới.
    */
    var hasOverlappingConfig =
        existingConfigs
            .Where(existing =>
                existing.Id !=
                    previousOpenEndedConfig?.Id)
            .Any(existing =>
                DateRangesOverlap(
                    request.EffectiveFrom,
                    request.EffectiveTo,
                    existing.EffectiveFrom,
                    existing.EffectiveTo));

    if (hasOverlappingConfig)
    {
        throw new InvalidOperationException(
            "Khoảng thời gian của cấu hình mới " +
            "bị chồng với một cấu hình khác.");
    }

    var now =
        GetVietnamNow();

    /*
      Tự đóng cấu hình cũ tại ngày liền trước
      ngày bắt đầu của cấu hình mới.

      Ví dụ:
      Cấu hình mới bắt đầu 01/09/2026
      → cấu hình cũ kết thúc 31/08/2026.
    */
    if (previousOpenEndedConfig != null)
    {
        var trackedPreviousConfig =
            await _repo
                .GetRateConfigByIdForUpdateAsync(
                    previousOpenEndedConfig.Id);

        if (trackedPreviousConfig == null)
        {
            throw new KeyNotFoundException(
                "Không tìm thấy cấu hình tỷ lệ cũ.");
        }

        trackedPreviousConfig.EffectiveTo =
            request.EffectiveFrom.AddDays(-1);

        trackedPreviousConfig.UpdatedAt =
            now;

        /*
          Không đặt IsActive = false ở đây.

          Cấu hình cũ vẫn còn hiệu lực cho đến
          ngày EffectiveTo vừa được thiết lập.
        */
    }

    var entity =
        new BhxhRateConfig
        {
            EmployeeRate =
                request.EmployeeRate,

            EmployerRate =
                request.EmployerRate,

            EffectiveFrom =
                request.EffectiveFrom,

            EffectiveTo =
                request.EffectiveTo,

            IsActive = true,

            CreatedByUserId =
                adminUserId,

            CreatedAt =
                now,

            UpdatedAt =
                now
        };

    await _repo
        .AddRateConfigAsync(entity);

    /*
      Một lần SaveChanges sẽ đồng thời:

      - Cập nhật ngày kết thúc cấu hình cũ.
      - Thêm cấu hình mới.
    */
    await _repo.SaveChangesAsync();

    var savedEntity =
        await _repo
            .GetRateConfigByIdAsync(
                entity.Id)
        ?? entity;

    return MapRateConfig(
        savedEntity,
        hasBeenUsed: false);
}

    /// <summary>
/// Cập nhật cấu hình tỷ lệ BHXH.
///
/// Chỉ được phép sửa khi:
/// - Cấu hình vẫn đang hoạt động.
/// - Chưa đến ngày bắt đầu hiệu lực.
/// - Chưa có khoản đóng nào sử dụng cấu hình.
/// </summary>
public async Task<BhxhRateConfigDto>
    UpdateRateConfigAsync(
        int rateConfigId,
        UpdateBhxhRateConfigRequest request,
        int adminUserId)
{
    // Kiểm tra người thực hiện thao tác có tồn tại.
    await EnsureActorExistsAsync(
        adminUserId);

    if (rateConfigId <= 0)
    {
        throw new ArgumentException(
            "Mã cấu hình tỷ lệ không hợp lệ.");
    }

    /*
      Lấy Entity có tracking.

      Khi các thuộc tính của entity thay đổi,
      SaveChangesAsync sẽ cập nhật xuống database.
    */
    var entity =
        await _repo
            .GetRateConfigByIdForUpdateAsync(
                rateConfigId);

    if (entity == null)
    {
        throw new KeyNotFoundException(
            "Không tìm thấy cấu hình tỷ lệ BHXH.");
    }

    /*
      Cấu hình đã ngừng chỉ được giữ lại
      để xem lịch sử, không được sửa lại.
    */
    if (!entity.IsActive)
    {
        throw new InvalidOperationException(
            "Cấu hình tỷ lệ đã ngừng áp dụng " +
            "nên không thể chỉnh sửa.");
    }

    var today =
        GetVietnamToday();

    /*
      Chỉ cấu hình có ngày hiệu lực
      sau ngày hiện tại mới được sửa.

      EffectiveFrom bằng hôm nay:
      → đã bắt đầu có hiệu lực
      → không được sửa.
    */
    if (entity.EffectiveFrom <= today)
    {
        throw new InvalidOperationException(
            "Cấu hình tỷ lệ đã đến ngày hiệu lực. " +
            "Hãy ngừng cấu hình cũ và tạo " +
            "một cấu hình mới.");
    }

    /*
      Kiểm tra cấu hình đã từng được dùng
      để sinh khoản đóng hay chưa.

      Kể cả khoản đóng còn DRAFT thì cấu hình
      vẫn được xem là đã sử dụng.
    */
    var hasBeenUsed =
        await _repo
            .RateConfigHasContributionsAsync(
                rateConfigId);

    if (hasBeenUsed)
    {
        throw new InvalidOperationException(
            "Cấu hình tỷ lệ đã được dùng để sinh " +
            "khoản đóng BHXH nên không thể chỉnh sửa.");
    }

    // Kiểm tra tỷ lệ nhân viên đóng.
    if (
        request.EmployeeRate < 0 ||
        request.EmployeeRate > 100
    )
    {
        throw new ArgumentException(
            "Tỷ lệ nhân viên phải nằm " +
            "trong khoảng từ 0 đến 100.");
    }

    // Kiểm tra tỷ lệ doanh nghiệp đóng.
    if (
        request.EmployerRate < 0 ||
        request.EmployerRate > 100
    )
    {
        throw new ArgumentException(
            "Tỷ lệ doanh nghiệp phải nằm " +
            "trong khoảng từ 0 đến 100.");
    }

    /*
      Ngày bắt đầu mới cũng phải nằm
      trong tương lai.
    */
    if (request.EffectiveFrom <= today)
    {
        throw new ArgumentException(
            "Ngày bắt đầu hiệu lực mới phải " +
            "là một ngày trong tương lai.");
    }

    // Ngày kết thúc không được trước ngày bắt đầu.
    ValidateDateRange(
        request.EffectiveFrom,
        request.EffectiveTo);

    /*
      Kiểm tra khoảng thời gian mới có chồng lấn
      với một cấu hình khác hay không.

      Phải loại cấu hình đang sửa khỏi danh sách.
    */
    var existingConfigs =
        await _repo
            .GetAllRateConfigsAsync();

    var hasOverlappingConfig =
        existingConfigs
            .Where(existing =>
                existing.Id != rateConfigId)
            .Any(existing =>
                DateRangesOverlap(
                    request.EffectiveFrom,
                    request.EffectiveTo,
                    existing.EffectiveFrom,
                    existing.EffectiveTo));

    if (hasOverlappingConfig)
    {
        throw new InvalidOperationException(
            "Khoảng thời gian sau khi cập nhật " +
            "bị chồng với một cấu hình khác.");
    }

    /*
      Cập nhật các thông tin được phép sửa.

      Không thay đổi:
      - CreatedByUserId.
      - CreatedAt.
    */
    entity.EmployeeRate =
        request.EmployeeRate;

    entity.EmployerRate =
        request.EmployerRate;

    entity.EffectiveFrom =
        request.EffectiveFrom;

    entity.EffectiveTo =
        request.EffectiveTo;

    entity.UpdatedAt =
        GetVietnamNow();

    await _repo.SaveChangesAsync();

    /*
      Đọc lại để có navigation CreatedByUser
      phục vụ việc trả tên người tạo ra DTO.
    */
    var savedEntity =
        await _repo
            .GetRateConfigByIdAsync(
                entity.Id)
        ?? entity;

    return MapRateConfig(
        savedEntity,
        hasBeenUsed: false);
}

    public async Task<BhxhRateConfigDto>
        DeactivateRateConfigAsync(
            int rateConfigId,
            DeactivateBhxhRateConfigRequest request,
            int adminUserId)
    {
        await EnsureActorExistsAsync(
            adminUserId);

        var entity =
            await _repo
                .GetRateConfigByIdForUpdateAsync(
                    rateConfigId);

        if (entity == null)
        {
            throw new KeyNotFoundException(
                "Không tìm thấy cấu hình tỷ lệ BHXH.");
        }

        if (!entity.IsActive)
        {
            throw new InvalidOperationException(
                "Cấu hình tỷ lệ này đã được ngừng sử dụng.");
        }

        if (request.EffectiveTo <
            entity.EffectiveFrom)
        {
            throw new ArgumentException(
                "Ngày kết thúc không được trước ngày bắt đầu hiệu lực.");
        }

        // Không xóa bản ghi.
        // Chỉ đánh dấu hết hiệu lực.
        entity.IsActive = false;
        entity.EffectiveTo =
            request.EffectiveTo;
        entity.UpdatedAt =
            GetVietnamNow();

        await _repo.SaveChangesAsync();

        var savedEntity =
            await _repo.GetRateConfigByIdAsync(
                entity.Id)
            ?? entity;

        return MapRateConfig(savedEntity);
    }


    // ========================================================
    // 3. HỒ SƠ BHXH NHÂN VIÊN
    // ========================================================

    public async Task<List<BhxhEmployeeProfileDto>>
        GetAllProfilesAsync()
    {
        var entities =
            await _repo.GetAllProfilesAsync();

        return entities
            .Select(MapProfile)
            .ToList();
    }

    public async Task<BhxhEmployeeProfileDto>
        GetProfileByIdAsync(int profileId)
    {
        var entity =
            await _repo.GetProfileByIdAsync(
                profileId);

        if (entity == null)
        {
            throw new KeyNotFoundException(
                "Không tìm thấy hồ sơ BHXH.");
        }

        return MapProfile(entity);
    }

    public async Task<BhxhEmployeeProfileDto>
        GetProfileByUserIdAsync(int userId)
    {
        var entity =
            await _repo.GetProfileByUserIdAsync(
                userId);

        if (entity == null)
        {
            throw new KeyNotFoundException(
                "Nhân viên chưa có hồ sơ BHXH.");
        }

        return MapProfile(entity);
    }

    public async Task<BhxhEmployeeProfileDto>
        CreateProfileAsync(
            CreateBhxhEmployeeProfileRequest request,
            int adminUserId)
    {
        await EnsureActorExistsAsync(
            adminUserId);

        var user =
            await _repo.GetUserByIdAsync(
                request.UserId);

        if (user == null)
        {
            throw new KeyNotFoundException(
                "Không tìm thấy nhân viên.");
        }

        EnsureFullTimeEmployee(user);

        var profileExists =
            await _repo.ProfileExistsForUserAsync(
                request.UserId);

        if (profileExists)
        {
            throw new InvalidOperationException(
                "Nhân viên này đã có hồ sơ BHXH.");
        }

        if (request.InsuranceSalaryBasis <= 0)
        {
            throw new ArgumentException(
                "Mức lương làm căn cứ đóng phải lớn hơn 0.");
        }

        ValidateDateRange(
            request.StartDate,
            request.EndDate);

        var normalizedNumber =
            NormalizeNullableText(
                request.SocialInsuranceNumber);

        if (normalizedNumber != null)
        {
            var numberExists =
                await _repo
                    .SocialInsuranceNumberExistsAsync(
                        normalizedNumber);

            if (numberExists)
            {
                throw new InvalidOperationException(
                    "Mã số BHXH đã được sử dụng cho nhân viên khác.");
            }
        }

        var now = GetVietnamNow();

        var entity =
            new BhxhEmployeeProfile
            {
                UserId =
                    request.UserId,
                SocialInsuranceNumber =
                    normalizedNumber,
                InsuranceSalaryBasis =
                    request.InsuranceSalaryBasis,
                StartDate =
                    request.StartDate,
                EndDate =
                    request.EndDate,

                // Hồ sơ mới chưa tự động được tính BHXH.
                // Admin phải kiểm tra rồi chuyển sang ACTIVE.
                Status =
                    PendingStatus,

                Note =
                    NormalizeNullableText(
                        request.Note),
                CreatedByUserId =
                    adminUserId,
                UpdatedByUserId =
                    adminUserId,
                CreatedAt = now,
                UpdatedAt = now
            };

        await _repo.AddProfileAsync(entity);
        await _repo.SaveChangesAsync();

        var savedEntity =
            await _repo.GetProfileByIdAsync(
                entity.Id)
            ?? entity;

        return MapProfile(savedEntity);
    }

    public async Task<BhxhEmployeeProfileDto>
        UpdateProfileAsync(
            int profileId,
            UpdateBhxhEmployeeProfileRequest request,
            int adminUserId)
    {
        await EnsureActorExistsAsync(
            adminUserId);

        var entity =
            await _repo
                .GetProfileByIdForUpdateAsync(
                    profileId);

        if (entity == null)
        {
            throw new KeyNotFoundException(
                "Không tìm thấy hồ sơ BHXH.");
        }

        // Nhân viên phải tiếp tục là FULL_TIME
        // khi cập nhật hồ sơ tham gia.
        EnsureFullTimeEmployee(entity.User);

        if (request.InsuranceSalaryBasis <= 0)
        {
            throw new ArgumentException(
                "Mức lương làm căn cứ đóng phải lớn hơn 0.");
        }

        ValidateDateRange(
            request.StartDate,
            request.EndDate);

        var normalizedNumber =
            NormalizeNullableText(
                request.SocialInsuranceNumber);

        if (normalizedNumber != null)
        {
            var numberExists =
                await _repo
                    .SocialInsuranceNumberExistsAsync(
                        normalizedNumber,
                        profileId);

            if (numberExists)
            {
                throw new InvalidOperationException(
                    "Mã số BHXH đã được sử dụng cho nhân viên khác.");
            }
        }

        entity.SocialInsuranceNumber =
            normalizedNumber;
        entity.InsuranceSalaryBasis =
            request.InsuranceSalaryBasis;
        entity.StartDate =
            request.StartDate;
        entity.EndDate =
            request.EndDate;
        entity.Note =
            NormalizeNullableText(
                request.Note);
        entity.UpdatedByUserId =
            adminUserId;
        entity.UpdatedAt =
            GetVietnamNow();

        await _repo.SaveChangesAsync();

        var savedEntity =
            await _repo.GetProfileByIdAsync(
                entity.Id)
            ?? entity;

        return MapProfile(savedEntity);
    }

    public async Task<BhxhEmployeeProfileDto>
        UpdateProfileStatusAsync(
            int profileId,
            UpdateBhxhProfileStatusRequest request,
            int adminUserId)
    {
        await EnsureActorExistsAsync(
            adminUserId);

        if (string.IsNullOrWhiteSpace(
                request.Status))
        {
            throw new ArgumentException(
                "Trạng thái hồ sơ là bắt buộc.");
        }

        var normalizedStatus =
            request.Status
                .Trim()
                .ToUpperInvariant();

        var allowedStatuses =
            new[]
            {
                PendingStatus,
                ActiveStatus,
                SuspendedStatus,
                StoppedStatus
            };

        if (!allowedStatuses.Contains(
                normalizedStatus))
        {
            throw new ArgumentException(
                "Trạng thái hồ sơ không hợp lệ.");
        }

        var entity =
            await _repo
                .GetProfileByIdForUpdateAsync(
                    profileId);

        if (entity == null)
        {
            throw new KeyNotFoundException(
                "Không tìm thấy hồ sơ BHXH.");
        }

        if (normalizedStatus == ActiveStatus)
        {
            EnsureFullTimeEmployee(entity.User);

            if (string.IsNullOrWhiteSpace(
                    entity.SocialInsuranceNumber))
            {
                throw new InvalidOperationException(
                    "Phải có mã số BHXH trước khi kích hoạt hồ sơ.");
            }

            if (entity.InsuranceSalaryBasis <= 0)
            {
                throw new InvalidOperationException(
                    "Mức lương làm căn cứ đóng phải lớn hơn 0.");
            }

            if (entity.EndDate.HasValue &&
                entity.EndDate.Value <
                GetVietnamToday())
            {
                throw new InvalidOperationException(
                    "Hồ sơ đã hết thời gian tham gia, " +
                    "không thể chuyển sang ACTIVE.");
            }
        }

        if (normalizedStatus == StoppedStatus)
        {
            var today = GetVietnamToday();

            // Khi ngừng hồ sơ, tự đặt ngày kết thúc
            // nếu hồ sơ chưa có hoặc đang có ngày tương lai.
            if (!entity.EndDate.HasValue ||
                entity.EndDate.Value > today)
            {
                entity.EndDate = today;
            }
        }

        entity.Status =
            normalizedStatus;

        var normalizedNote =
            NormalizeNullableText(request.Note);

        // Không truyền ghi chú mới thì giữ ghi chú cũ.
        if (normalizedNote != null)
        {
            entity.Note = normalizedNote;
        }

        entity.UpdatedByUserId =
            adminUserId;
        entity.UpdatedAt =
            GetVietnamNow();

        await _repo.SaveChangesAsync();

        var savedEntity =
            await _repo.GetProfileByIdAsync(
                entity.Id)
            ?? entity;

        return MapProfile(savedEntity);
    }


    // ========================================================
    // 4. KHOẢN ĐÓNG BHXH HẰNG THÁNG
    // ========================================================

    public async Task<
        GenerateBhxhContributionsResultDto>
        GenerateContributionsAsync(
            GenerateBhxhContributionsRequest request)
    {
        ValidatePeriod(
            request.Month,
            request.Year);

        // Trong đồ án, tỷ lệ của tháng được xác định
        // theo ngày đầu tiên của tháng.
        var targetDate =
            new DateOnly(
                request.Year,
                request.Month,
                1);

        var rateConfig =
            await _repo
                .GetEffectiveRateConfigAsync(
                    targetDate);

        if (rateConfig == null)
        {
            throw new InvalidOperationException(
                "Không tìm thấy cấu hình tỷ lệ BHXH " +
                "có hiệu lực trong tháng được chọn.");
        }

        var eligibleProfiles =
            await _repo
                .GetActiveProfilesForPeriodAsync(
                    request.Month,
                    request.Year);

        // Đọc các khoản đã có trong tháng một lần,
        // tránh truy vấn database riêng cho từng nhân viên.
        var existingContributions =
            await _repo
                .GetContributionsByPeriodAsync(
                    request.Month,
                    request.Year);

        var existingUserIds =
            existingContributions
                .Select(item => item.UserId)
                .ToHashSet();

        var newContributions =
            new List<BhxhMonthlyContribution>();

        var skippedExistingCount = 0;

        var now = GetVietnamNow();

        foreach (var profile in eligibleProfiles)
        {
            if (existingUserIds.Contains(
                    profile.UserId))
            {
                skippedExistingCount++;
                continue;
            }

            if (profile.InsuranceSalaryBasis <= 0)
            {
                throw new InvalidOperationException(
                    $"Hồ sơ BHXH của nhân viên " +
                    $"{profile.User.FullName} có mức lương " +
                    "làm căn cứ không hợp lệ.");
            }

            // Tiền nhân viên đóng =
            // mức lương căn cứ × tỷ lệ nhân viên / 100.
            var employeeAmount =
                Math.Round(
                    profile.InsuranceSalaryBasis *
                    rateConfig.EmployeeRate /
                    100m,
                    2,
                    MidpointRounding.AwayFromZero);

            // Tiền doanh nghiệp đóng =
            // mức lương căn cứ × tỷ lệ doanh nghiệp / 100.
            var employerAmount =
                Math.Round(
                    profile.InsuranceSalaryBasis *
                    rateConfig.EmployerRate /
                    100m,
                    2,
                    MidpointRounding.AwayFromZero);

            var totalAmount =
                employeeAmount +
                employerAmount;

            newContributions.Add(
                new BhxhMonthlyContribution
                {
                    UserId =
                        profile.UserId,
                    ProfileId =
                        profile.Id,
                    RateConfigId =
                        rateConfig.Id,

                    // Entity đang dùng sbyte và short
                    // vì cột MySQL là TINYINT và SMALLINT.
                    Month =
                        (sbyte)request.Month,
                    Year =
                        (short)request.Year,

                    // Lưu snapshot để lịch sử không thay đổi
                    // khi hồ sơ hoặc tỷ lệ được sửa sau này.
                    InsuranceSalaryBasis =
                        profile.InsuranceSalaryBasis,
                    EmployeeRate =
                        rateConfig.EmployeeRate,
                    EmployerRate =
                        rateConfig.EmployerRate,
                    EmployeeAmount =
                        employeeAmount,
                    EmployerAmount =
                        employerAmount,
                    TotalAmount =
                        totalAmount,
                    Status =
                        DraftStatus,
                    CreatedAt = now,
                    UpdatedAt = now
                });
        }

        if (newContributions.Count > 0)
        {
            await _repo.AddContributionsAsync(
                newContributions);

            await _repo.SaveChangesAsync();
        }

        return new GenerateBhxhContributionsResultDto
        {
            Month = request.Month,
            Year = request.Year,
            CreatedCount =
                newContributions.Count,
            SkippedExistingCount =
                skippedExistingCount,

            // Repository đã lọc sẵn các hồ sơ
            // nằm ngoài thời gian tham gia.
            SkippedOutOfPeriodCount = 0,

            Message =
                $"Đã tạo {newContributions.Count} " +
                "khoản đóng BHXH; " +
                $"bỏ qua {skippedExistingCount} " +
                "khoản đã tồn tại."
        };
    }

    public async Task<
        List<BhxhMonthlyContributionDto>>
        GetContributionsByPeriodAsync(
            int month,
            int year)
    {
        ValidatePeriod(month, year);

        var entities =
            await _repo
                .GetContributionsByPeriodAsync(
                    month,
                    year);

        return entities
            .Select(MapContribution)
            .ToList();
    }

    public async Task<
        List<BhxhMonthlyContributionDto>>
        GetContributionsByUserIdAsync(
            int userId)
    {
        var user =
            await _repo.GetUserByIdAsync(
                userId);

        if (user == null)
        {
            throw new KeyNotFoundException(
                "Không tìm thấy nhân viên.");
        }

        var entities =
            await _repo
                .GetContributionsByUserIdAsync(
                    userId);

        return entities
            .Select(MapContribution)
            .ToList();
    }

    public async Task<BhxhMonthlyContributionDto>
        GetContributionByIdAsync(
            int contributionId)
    {
        var entity =
            await _repo
                .GetContributionByIdAsync(
                    contributionId);

        if (entity == null)
        {
            throw new KeyNotFoundException(
                "Không tìm thấy khoản đóng BHXH.");
        }

        return MapContribution(entity);
    }

    public async Task<BhxhMonthlyContributionDto>
        ConfirmContributionAsync(
            int contributionId,
            int adminUserId)
    {
        await EnsureActorExistsAsync(
            adminUserId);

        var entity =
            await _repo
                .GetContributionByIdForUpdateAsync(
                    contributionId);

        if (entity == null)
        {
            throw new KeyNotFoundException(
                "Không tìm thấy khoản đóng BHXH.");
        }

        if (!string.Equals(
                entity.Status,
                DraftStatus,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Chỉ khoản đóng ở trạng thái DRAFT " +
                "mới được xác nhận.");
        }

        var now = GetVietnamNow();

        entity.Status =
            ConfirmedStatus;
        entity.ConfirmedByUserId =
            adminUserId;
        entity.ConfirmedAt = now;
        entity.UpdatedAt = now;

        await _repo.SaveChangesAsync();

        var savedEntity =
            await _repo
                .GetContributionByIdAsync(
                    entity.Id)
            ?? entity;

        return MapContribution(savedEntity);
    }

    public async Task<BhxhMonthlyContributionDto>
        MarkContributionPaidAsync(
            int contributionId,
            int adminUserId)
    {
        await EnsureActorExistsAsync(
            adminUserId);

        var entity =
            await _repo
                .GetContributionByIdForUpdateAsync(
                    contributionId);

        if (entity == null)
        {
            throw new KeyNotFoundException(
                "Không tìm thấy khoản đóng BHXH.");
        }

        if (!string.Equals(
                entity.Status,
                ConfirmedStatus,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Chỉ khoản đóng đã CONFIRMED " +
                "mới được chuyển sang PAID.");
        }

        var now = GetVietnamNow();

        entity.Status =
            PaidStatus;
        entity.PaidByUserId =
            adminUserId;
        entity.PaidAt = now;
        entity.UpdatedAt = now;

        await _repo.SaveChangesAsync();

        var savedEntity =
            await _repo
                .GetContributionByIdAsync(
                    entity.Id)
            ?? entity;

        return MapContribution(savedEntity);
    }

    public async Task<BhxhMonthlyContributionDto>
        CancelContributionAsync(
            int contributionId,
            CancelBhxhContributionRequest request,
            int adminUserId)
    {
        await EnsureActorExistsAsync(
            adminUserId);

        if (string.IsNullOrWhiteSpace(
                request.Reason))
        {
            throw new ArgumentException(
                "Lý do hủy khoản đóng là bắt buộc.");
        }

        var reason =
            request.Reason.Trim();

        if (reason.Length > 500)
        {
            throw new ArgumentException(
                "Lý do hủy không được vượt quá 500 ký tự.");
        }

        var entity =
            await _repo
                .GetContributionByIdForUpdateAsync(
                    contributionId);

        if (entity == null)
        {
            throw new KeyNotFoundException(
                "Không tìm thấy khoản đóng BHXH.");
        }

        if (string.Equals(
                entity.Status,
                PaidStatus,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Khoản đóng đã PAID không thể hủy.");
        }

        if (string.Equals(
                entity.Status,
                CancelledStatus,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Khoản đóng này đã bị hủy trước đó.");
        }

        entity.Status =
            CancelledStatus;

        // Bảng hiện tại chưa có cancelled_by_user_id
        // và cancelled_at, nên tạm lưu người hủy cùng lý do
        // trong trường Note để vẫn có thể truy vết.
        var cancellationNote =
            $"Hủy bởi người dùng ID {adminUserId}: {reason}";

        entity.Note =
            string.IsNullOrWhiteSpace(entity.Note)
                ? cancellationNote
                : entity.Note.Trim() +
                  " | " +
                  cancellationNote;

        entity.UpdatedAt =
            GetVietnamNow();

        await _repo.SaveChangesAsync();

        var savedEntity =
            await _repo
                .GetContributionByIdAsync(
                    entity.Id)
            ?? entity;

        return MapContribution(savedEntity);
    }
}
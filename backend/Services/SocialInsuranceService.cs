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
public partial class SocialInsuranceService
    : ISocialInsuranceService
{
    private const string FullTimeType = "FULL_TIME";
    private const string MaternityType = "MATERNITY";

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
                StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(
                user.EmploymentType,
                MaternityType,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Chỉ nhân viên FULL_TIME hoặc Thai sản mới được tham gia BHXH.");
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
}

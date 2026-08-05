using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Repositories;

namespace LuanVanTotNghiep.Services;

// Xử lý các nghiệp vụ dùng chung của phân hệ BHXH.
//
// Service chịu trách nhiệm:
// - Kiểm tra dữ liệu đầu vào.
// - Kiểm tra nhân viên FULL_TIME.
// - Kiểm tra trạng thái hồ sơ.
// - Tính tiền đóng BHXH.
// - Chuyển Entity sang DTO.
// - Không xóa cứng dữ liệu.
//
// Các chức năng chi tiết được chia thành các file partial:
// - SocialInsuranceService.Employees.cs
// - SocialInsuranceService.Profiles.cs
// - SocialInsuranceService.RateConfigs.cs
// - SocialInsuranceService.Contributions.cs
public partial class SocialInsuranceService
    : ISocialInsuranceService
{
    // Theo nghiệp vụ của nhóm,
    // chỉ nhân viên FULL_TIME tham gia BHXH.
    private const string FullTimeType =
        "FULL_TIME";

    // Trạng thái hồ sơ do Admin quản lý.
    private const string PendingStatus =
        "PENDING";

    private const string ActiveStatus =
        "ACTIVE";

    private const string SuspendedStatus =
        "SUSPENDED";

    private const string StoppedStatus =
        "STOPPED";

    // Trạng thái Staff kiểm tra hồ sơ.
    private const string StaffConfirmationPending =
        "PENDING";

    private const string StaffConfirmationConfirmed =
        "CONFIRMED";

    private const string StaffConfirmationChangeRequested =
        "CHANGE_REQUESTED";

    // Trạng thái khoản đóng BHXH hằng tháng.
    private const string DraftStatus =
        "DRAFT";

    private const string ConfirmedStatus =
        "CONFIRMED";

    private const string PaidStatus =
        "PAID";

    private const string CancelledStatus =
        "CANCELLED";

    private readonly ISocialInsuranceRepo _repo;

    // Repository được Dependency Injection truyền vào.
    public SocialInsuranceService(
        ISocialInsuranceRepo repo)
    {
        _repo = repo;
    }


    // ========================================================
    // HÀM HỖ TRỢ THỜI GIAN VIỆT NAM
    // ========================================================

    // Tìm múi giờ Việt Nam.
    //
    // Windows thường dùng:
    // SE Asia Standard Time
    //
    // Linux thường dùng:
    // Asia/Ho_Chi_Minh
    private static TimeZoneInfo GetVietnamTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(
                "SE Asia Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById(
                "Asia/Ho_Chi_Minh");
        }
    }

    // Lấy thời gian hiện tại theo múi giờ Việt Nam.
    private static DateTime GetVietnamNow()
    {
        return TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            GetVietnamTimeZone());
    }

    // Lấy ngày hiện tại theo múi giờ Việt Nam.
    private static DateOnly GetVietnamToday()
    {
        return DateOnly.FromDateTime(
            GetVietnamNow());
    }


    // ========================================================
    // HÀM KIỂM TRA DỮ LIỆU DÙNG CHUNG
    // ========================================================

    // Chuẩn hóa chuỗi:
    // - Xóa khoảng trắng đầu và cuối.
    // - Chuỗi rỗng được chuyển thành NULL.
    private static string? NormalizeNullableText(
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    // Kiểm tra nhân viên có đủ điều kiện
    // tham gia BHXH theo nghiệp vụ của nhóm hay không.
    //
    // Chỉ nhân viên FULL_TIME và chưa bị xóa
    // mới được tạo hoặc kích hoạt hồ sơ BHXH.
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

    // Kiểm tra người thực hiện thao tác có tồn tại.
    //
    // Việc kiểm tra người đó có đúng vai trò Admin hoặc Staff
    // được thực hiện thêm tại Controller bằng Authorize.
    private async Task EnsureActorExistsAsync(
        int userId)
    {
        if (userId <= 0)
        {
            throw new ArgumentException(
                "Người thực hiện thao tác không hợp lệ.");
        }

        var user =
            await _repo.GetUserByIdAsync(
                userId);

        if (user == null)
        {
            throw new KeyNotFoundException(
                "Không tìm thấy người thực hiện thao tác.");
        }
    }

    // Kiểm tra ngày kết thúc
    // không được đứng trước ngày bắt đầu.
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

    // Kiểm tra hai khoảng thời gian
    // có chồng lên nhau hay không.
    //
    // EndDate bằng NULL được hiểu là
    // chưa xác định ngày kết thúc.
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

    // Kiểm tra tháng và năm hợp lệ.
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

    // Chuyển Entity cấu hình tỷ lệ thành DTO.
    //
    // hasBeenUsed:
    // - true: cấu hình đã được dùng sinh khoản đóng.
    // - false: cấu hình chưa được sử dụng.
    private static BhxhRateConfigDto MapRateConfig(
        BhxhRateConfig entity,
        bool hasBeenUsed = false)
    {
        var today =
            GetVietnamToday();

        // Chỉ cho phép chỉnh sửa khi:
        // 1. Cấu hình vẫn hoạt động.
        // 2. Ngày hiệu lực nằm trong tương lai.
        // 3. Cấu hình chưa được sử dụng.
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

    // Chuyển Entity hồ sơ BHXH sang DTO.
    //
    // Hàm này được dùng chung cho:
    // - Admin xem hồ sơ nhân viên.
    // - Staff xem hồ sơ của chính mình.
    private static BhxhEmployeeProfileDto MapProfile(
        BhxhEmployeeProfile entity)
    {
        return new BhxhEmployeeProfileDto
        {
            Id =
                entity.Id,

            UserId =
                entity.UserId,

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

            StartDate =
                entity.StartDate,

            EndDate =
                entity.EndDate,

            Status =
                entity.Status,

            // Trạng thái Staff kiểm tra hồ sơ.
            StaffConfirmationStatus =
                entity.StaffConfirmationStatus,

            // Thời điểm Staff xác nhận hồ sơ.
            StaffConfirmedAt =
                entity.StaffConfirmedAt,

            // Nội dung Staff yêu cầu Admin chỉnh sửa.
            StaffConfirmationNote =
                entity.StaffConfirmationNote,

            // Ghi chú nội bộ của Admin.
            Note =
                entity.Note,

            CreatedByUserId =
                entity.CreatedByUserId,

            CreatedByUserName =
                entity.CreatedByUser?.FullName,

            UpdatedByUserId =
                entity.UpdatedByUserId,

            UpdatedByUserName =
                entity.UpdatedByUser?.FullName,

            CreatedAt =
                entity.CreatedAt,

            UpdatedAt =
                entity.UpdatedAt
        };
    }

    // Chuyển Entity khoản đóng BHXH sang DTO.
    private static BhxhMonthlyContributionDto
        MapContribution(
            BhxhMonthlyContribution entity)
    {
        return new BhxhMonthlyContributionDto
        {
            Id =
                entity.Id,

            UserId =
                entity.UserId,

            FullName =
                entity.User?.FullName
                ?? string.Empty,

            ProfileId =
                entity.ProfileId,

            RateConfigId =
                entity.RateConfigId,

            Month =
                entity.Month,

            Year =
                entity.Year,

            InsuranceSalaryBasis =
                entity.InsuranceSalaryBasis,

            EmployeeRate =
                entity.EmployeeRate,

            EmployerRate =
                entity.EmployerRate,

            EmployeeAmount =
                entity.EmployeeAmount,
                EmployeeDeductedAmount =
    entity.EmployeeDeductedAmount,

EmployeeOutstandingAmount =
    entity.EmployeeOutstandingAmount,

DeductionStatus =
    entity.DeductionStatus,

            EmployerAmount =
                entity.EmployerAmount,

            TotalAmount =
                entity.TotalAmount,

            Status =
                entity.Status,

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

            PaidAt =
                entity.PaidAt,

            Note =
                entity.Note,

            CreatedAt =
                entity.CreatedAt,

            UpdatedAt =
                entity.UpdatedAt
        };
    }
}
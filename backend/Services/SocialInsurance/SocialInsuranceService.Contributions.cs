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
{
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


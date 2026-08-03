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
}


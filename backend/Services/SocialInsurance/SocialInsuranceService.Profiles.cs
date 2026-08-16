using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;

namespace LuanVanTotNghiep.Services;

// Xử lý nghiệp vụ hồ sơ BHXH.
//
// Luồng chính:
// Admin tạo hồ sơ
// → Staff kiểm tra
// → Admin kích hoạt
// → Admin sinh khoản đóng hằng tháng.
public partial class SocialInsuranceService
{
    // ========================================================
    // 1. ĐỌC HỒ SƠ
    // ========================================================

    // Lấy toàn bộ hồ sơ cho Admin.
    public async Task<List<BhxhEmployeeProfileDto>>
        GetAllProfilesAsync()
    {
        var entities =
            await _repo.GetAllProfilesAsync();

        return entities
            .Select(MapProfile)
            .ToList();
    }

    // Lấy một hồ sơ theo ID.
    public async Task<BhxhEmployeeProfileDto>
        GetProfileByIdAsync(
            int profileId)
    {
        if (profileId <= 0)
        {
            throw new ArgumentException(
                "ID hồ sơ không hợp lệ.");
        }

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

    // Lấy hồ sơ theo ID nhân viên.
    //
    // Controller phải bảo đảm Staff chỉ truyền
    // ID của chính mình từ JWT.
    public async Task<BhxhEmployeeProfileDto>
        GetProfileByUserIdAsync(
            int userId)
    {
        if (userId <= 0)
        {
            throw new ArgumentException(
                "Nhân viên không hợp lệ.");
        }

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


    // ========================================================
    // 2. ADMIN TẠO HỒ SƠ
    // ========================================================

    public async Task<BhxhEmployeeProfileDto>
        CreateProfileAsync(
            CreateBhxhEmployeeProfileRequest request,
            int adminUserId)
    {
        if (request == null)
        {
            throw new ArgumentNullException(
                nameof(request));
        }

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

        // Theo nghiệp vụ của nhóm,
        // chỉ FULL TIME tham gia BHXH.
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

        var now =
            GetVietnamNow();

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

                // Hồ sơ mới chưa được kích hoạt.
                Status =
                    PendingStatus,

                // Staff chưa kiểm tra hồ sơ mới.
                StaffConfirmationStatus =
                    StaffConfirmationPending,

                StaffConfirmedAt =
                    null,

                StaffConfirmationNote =
                    null,

                Note =
                    NormalizeNullableText(
                        request.Note),

                CreatedByUserId =
                    adminUserId,

                UpdatedByUserId =
                    adminUserId,

                CreatedAt =
                    now,

                UpdatedAt =
                    now
            };

        await _repo.AddProfileAsync(
            entity);

        await _repo.SaveChangesAsync();

        var savedEntity =
            await _repo.GetProfileByIdAsync(
                entity.Id)
            ?? entity;

        return MapProfile(savedEntity);
    }


    // ========================================================
    // 3. ADMIN CẬP NHẬT HỒ SƠ
    // ========================================================

    public async Task<BhxhEmployeeProfileDto>
        UpdateProfileAsync(
            int profileId,
            UpdateBhxhEmployeeProfileRequest request,
            int adminUserId)
    {
        if (request == null)
        {
            throw new ArgumentNullException(
                nameof(request));
        }

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

        // Nhân viên phải tiếp tục là FULL TIME.
        EnsureFullTimeEmployee(
            entity.User);

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

        // Kiểm tra Admin có thay đổi thông tin
        // mà Staff cần xác nhận lại hay không.
        var importantInformationChanged =
            !string.Equals(
                entity.SocialInsuranceNumber,
                normalizedNumber,
                StringComparison.Ordinal) ||

            entity.InsuranceSalaryBasis !=
                request.InsuranceSalaryBasis ||

            entity.StartDate !=
                request.StartDate ||

            entity.EndDate !=
                request.EndDate;

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

        // Khi Admin sửa thông tin quan trọng:
        // - Hồ sơ trở lại PENDING.
        // - Xác nhận Staff trở lại PENDING.
        // - Xóa thời điểm và phản hồi cũ.
        //
        // Nếu Admin chỉ sửa ghi chú nội bộ,
        // xác nhận của Staff được giữ nguyên.
        if (importantInformationChanged)
        {
            entity.Status =
                PendingStatus;

            entity.StaffConfirmationStatus =
                StaffConfirmationPending;

            entity.StaffConfirmedAt =
                null;

            entity.StaffConfirmationNote =
                null;
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
    // 4. STAFF KIỂM TRA HỒ SƠ CỦA CHÍNH MÌNH
    // ========================================================

    public async Task<BhxhEmployeeProfileDto>
        UpdateMyProfileConfirmationAsync(
            int staffUserId,
            UpdateMyBhxhConfirmationRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(
                nameof(request));
        }

        await EnsureActorExistsAsync(
            staffUserId);

        if (string.IsNullOrWhiteSpace(
                request.ConfirmationStatus))
        {
            throw new ArgumentException(
                "Trạng thái xác nhận là bắt buộc.");
        }

        var normalizedStatus =
            request.ConfirmationStatus
                .Trim()
                .ToUpperInvariant();

        // Staff chỉ được xác nhận
        // hoặc yêu cầu Admin chỉnh sửa.
        if (normalizedStatus !=
                StaffConfirmationConfirmed &&
            normalizedStatus !=
                StaffConfirmationChangeRequested)
        {
            throw new ArgumentException(
                "Staff chỉ được chọn CONFIRMED " +
                "hoặc CHANGE_REQUESTED.");
        }

        var entity =
            await _repo
                .GetProfileByUserIdForUpdateAsync(
                    staffUserId);

        if (entity == null)
        {
            throw new KeyNotFoundException(
                "Bạn chưa có hồ sơ BHXH.");
        }

        EnsureFullTimeEmployee(
            entity.User);
            // Khi Staff đã yêu cầu chỉnh sửa,
// Staff phải chờ Admin cập nhật hồ sơ.
//
// Sau khi Admin sửa thông tin quan trọng,
// StaffConfirmationStatus sẽ được đưa về PENDING
// và Staff mới được kiểm tra lại.
if (entity.StaffConfirmationStatus ==
    StaffConfirmationChangeRequested)
{
    throw new InvalidOperationException(
        "Bạn đã yêu cầu chỉnh sửa hồ sơ. " +
        "Vui lòng chờ Admin cập nhật trước khi xác nhận lại.");
}

        // Staff chỉ kiểm tra hồ sơ đang chờ xử lý.
        //
        // Khi hồ sơ đã ACTIVE, SUSPENDED hoặc STOPPED,
        // Staff không được tự thay đổi xác nhận nữa.
        if (entity.Status != PendingStatus)
        {
            throw new InvalidOperationException(
                "Chỉ hồ sơ đang PENDING mới được xác nhận.");
        }

        var normalizedNote =
            NormalizeNullableText(
                request.Note);

        // Khi yêu cầu chỉnh sửa,
        // Staff phải nêu rõ nội dung cần sửa.
        if (normalizedStatus ==
                StaffConfirmationChangeRequested &&
            normalizedNote == null)
        {
            throw new ArgumentException(
                "Bạn phải nhập nội dung cần Admin chỉnh sửa.");
        }

        entity.StaffConfirmationStatus =
            normalizedStatus;

        if (normalizedStatus ==
            StaffConfirmationConfirmed)
        {
            // Staff xác nhận thông tin đúng.
            entity.StaffConfirmedAt =
                GetVietnamNow();

            // Xóa phản hồi yêu cầu sửa trước đó.
            entity.StaffConfirmationNote =
                null;
        }
        else
        {
            // Staff yêu cầu Admin sửa thông tin.
            entity.StaffConfirmedAt =
                null;

            entity.StaffConfirmationNote =
                normalizedNote;
        }

        // Không cập nhật UpdatedByUserId tại đây,
        // vì trường đó dùng để lưu Admin chỉnh sửa hồ sơ.
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
    // 5. ADMIN CHUYỂN TRẠNG THÁI HỒ SƠ
    // ========================================================

    public async Task<BhxhEmployeeProfileDto>
        UpdateProfileStatusAsync(
            int profileId,
            UpdateBhxhProfileStatusRequest request,
            int adminUserId)
    {
        if (request == null)
        {
            throw new ArgumentNullException(
                nameof(request));
        }

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

        if (normalizedStatus ==
            ActiveStatus)
        {
            EnsureFullTimeEmployee(
                entity.User);

            // Staff phải xác nhận trước
            // khi Admin kích hoạt hồ sơ.
            if (entity.StaffConfirmationStatus !=
                StaffConfirmationConfirmed)
            {
                throw new InvalidOperationException(
                    "Staff phải xác nhận hồ sơ trước " +
                    "khi Admin chuyển sang ACTIVE.");
            }

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

            ValidateDateRange(
                entity.StartDate,
                entity.EndDate);

            if (entity.EndDate.HasValue &&
                entity.EndDate.Value <
                    GetVietnamToday())
            {
                throw new InvalidOperationException(
                    "Hồ sơ đã hết thời gian tham gia, " +
                    "không thể chuyển sang ACTIVE.");
            }
        }

        if (normalizedStatus ==
            StoppedStatus)
        {
            var today =
                GetVietnamToday();

            // Khi ngừng hồ sơ, tự đặt ngày kết thúc
            // nếu chưa có hoặc đang có ngày tương lai.
            if (!entity.EndDate.HasValue ||
                entity.EndDate.Value > today)
            {
                entity.EndDate =
                    today;
            }
        }

        entity.Status =
            normalizedStatus;

        var normalizedNote =
            NormalizeNullableText(
                request.Note);

        // Không truyền ghi chú mới
        // thì giữ nguyên ghi chú cũ.
        if (normalizedNote != null)
        {
            entity.Note =
                normalizedNote;
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
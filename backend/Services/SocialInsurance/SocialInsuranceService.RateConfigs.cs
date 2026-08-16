using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Repositories;

namespace LuanVanTotNghiep.Services;

///  
/// Xử lý toàn bộ nghiệp vụ BHXH.
///
/// Service chịu trách nhiệm:
/// - Kiểm tra dữ liệu.
/// - Kiểm tra nhân viên FULL TIME.
/// - Tính tiền đóng BHXH.
/// - Chuyển trạng thái.
/// - Không xóa cứng dữ liệu.
///
/// Repository chỉ chịu trách nhiệm đọc và lưu database.
///  
public partial class SocialInsuranceService
{
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
}


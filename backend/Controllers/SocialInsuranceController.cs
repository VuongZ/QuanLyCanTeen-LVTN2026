using System.Security.Claims;
using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LuanVanTotNghiep.Controllers;

/// <summary>
/// Controller phụ trách các API bảo hiểm xã hội.
///
/// Phạm vi:
///
/// Admin:
/// - Quản lý nhân viên FULL_TIME.
/// - Quản lý cấu hình tỷ lệ.
/// - Quản lý hồ sơ BHXH.
/// - Sinh, xác nhận, thanh toán và hủy khoản đóng.
///
/// Staff:
/// - Xem hồ sơ BHXH của chính mình.
/// - Xem lịch sử đóng BHXH của chính mình.
///
/// Manager không có chức năng BHXH.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SocialInsuranceController : ControllerBase
{
    private readonly ISocialInsuranceService _service;

    private readonly ILogger<SocialInsuranceController>
        _logger;

    /// <summary>
    /// Nhận Service và Logger thông qua
    /// Dependency Injection.
    /// </summary>
    public SocialInsuranceController(
        ISocialInsuranceService service,
        ILogger<SocialInsuranceController> logger)
    {
        _service = service;
        _logger = logger;
    }


    // ========================================================
    // HÀM HỖ TRỢ
    // ========================================================

    /// <summary>
    /// Lấy ID người đang đăng nhập từ JWT.
    ///
    /// Không nhận AdminId hoặc StaffId từ Frontend,
    /// vì người dùng có thể giả mạo ID của tài khoản khác.
    /// </summary>
    private int GetCurrentUserId()
    {
        var userIdValue =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!int.TryParse(
                userIdValue,
                out var userId) ||
            userId <= 0)
        {
            throw new UnauthorizedAccessException(
                "Không xác định được người dùng hiện tại.");
        }

        return userId;
    }

    /// <summary>
    /// Chuyển Exception từ Service thành mã HTTP phù hợp.
    ///
    /// KeyNotFoundException:
    ///     404 Not Found.
    ///
    /// ArgumentException và InvalidOperationException:
    ///     400 Bad Request.
    ///
    /// UnauthorizedAccessException:
    ///     401 Unauthorized.
    ///
    /// Các lỗi không xác định:
    ///     500 Internal Server Error.
    /// </summary>
    private IActionResult HandleException(
        Exception exception,
        string actionName)
    {
        if (exception is UnauthorizedAccessException)
        {
            return Unauthorized(new
            {
                message = exception.Message
            });
        }

        if (exception is KeyNotFoundException)
        {
            return NotFound(new
            {
                message = exception.Message
            });
        }

        if (exception is ArgumentException ||
            exception is InvalidOperationException)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }

        // Chỉ ghi chi tiết các lỗi không dự kiến
        // vào Terminal của Backend.
        _logger.LogError(
            exception,
            "Lỗi khi thực hiện chức năng BHXH: {ActionName}.",
            actionName);

        // Không trả toàn bộ chi tiết lỗi nội bộ
        // cho Frontend.
        return StatusCode(500, new
        {
            message =
                "Đã xảy ra lỗi trong quá trình xử lý BHXH."
        });
    }


    // ========================================================
    // 1. NHÂN VIÊN FULL_TIME - ADMIN
    // ========================================================

    /// <summary>
    /// Admin lấy danh sách nhân viên FULL_TIME.
    ///
    /// Kết quả cho biết:
    /// - Nhân viên đã có hồ sơ BHXH chưa.
    /// - Trạng thái hồ sơ hiện tại.
    ///
    /// GET:
    /// /api/SocialInsurance/full-time-employees
    /// </summary>
    [HttpGet("full-time-employees")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult>
        GetFullTimeEmployees()
    {
        try
        {
            var result =
                await _service
                    .GetFullTimeEmployeesAsync();

            return Ok(result);
        }
        catch (Exception ex)
        {
            return HandleException(
                ex,
                nameof(GetFullTimeEmployees));
        }
    }


    // ========================================================
    // 2. CẤU HÌNH TỶ LỆ BHXH - ADMIN
    // ========================================================

    /// <summary>
    /// Lấy toàn bộ lịch sử cấu hình tỷ lệ.
    ///
    /// GET:
    /// /api/SocialInsurance/rates
    /// </summary>
    [HttpGet("rates")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult>
        GetAllRateConfigs()
    {
        try
        {
            var result =
                await _service
                    .GetAllRateConfigsAsync();

            return Ok(result);
        }
        catch (Exception ex)
        {
            return HandleException(
                ex,
                nameof(GetAllRateConfigs));
        }
    }

    /// <summary>
    /// Admin tạo cấu hình tỷ lệ mới.
    ///
    /// POST:
    /// /api/SocialInsurance/rates
    ///
    /// AdminId được lấy từ JWT.
    /// </summary>
    [HttpPost("rates")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult>
        CreateRateConfig(
            [FromBody]
            CreateBhxhRateConfigRequest request)
    {
        try
        {
            var adminUserId =
                GetCurrentUserId();

            var result =
                await _service
                    .CreateRateConfigAsync(
                        request,
                        adminUserId);

            return Ok(new
            {
                message =
                    "Đã tạo cấu hình tỷ lệ BHXH thành công.",

                data = result
            });
        }
        catch (Exception ex)
        {
            return HandleException(
                ex,
                nameof(CreateRateConfig));
        }
    }

    /// <summary>
/// Admin cập nhật một cấu hình tỷ lệ BHXH.
///
/// Chỉ được chỉnh sửa khi:
/// - Cấu hình vẫn đang hoạt động.
/// - Chưa đến ngày bắt đầu hiệu lực.
/// - Chưa được dùng để sinh khoản đóng.
///
/// PUT:
/// /api/SocialInsurance/rates/{rateConfigId}
///
/// AdminId được lấy từ JWT.
/// </summary>
[HttpPut("rates/{rateConfigId:int}")]
[Authorize(Roles = "ADMIN")]
public async Task<IActionResult>
    UpdateRateConfig(
        int rateConfigId,
        [FromBody]
        UpdateBhxhRateConfigRequest request)
{
    try
    {
        if (rateConfigId <= 0)
        {
            return BadRequest(new
            {
                message =
                    "Mã cấu hình tỷ lệ không hợp lệ."
            });
        }

        // Không nhận AdminId từ Frontend.
        // ID Admin được lấy trực tiếp từ JWT.
        var adminUserId =
            GetCurrentUserId();

        var result =
            await _service
                .UpdateRateConfigAsync(
                    rateConfigId,
                    request,
                    adminUserId);

        return Ok(new
        {
            message =
                "Đã cập nhật cấu hình tỷ lệ BHXH thành công.",

            data = result
        });
    }
    catch (Exception ex)
    {
        return HandleException(
            ex,
            nameof(UpdateRateConfig));
    }
}


    /// Admin ngừng sử dụng một cấu hình tỷ lệ.
    ///
    /// PUT:
    /// /api/SocialInsurance/rates/{id}/deactivate
    ///
    /// Không xóa bản ghi khỏi database.
    /// Chỉ cập nhật:
    /// - IsActive = false.
    /// - EffectiveTo = ngày kết thúc.
    [HttpPut("rates/{rateConfigId:int}/deactivate")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult>
        DeactivateRateConfig(
            int rateConfigId,
            [FromBody]
            DeactivateBhxhRateConfigRequest request)
    {
        try
        {
            if (rateConfigId <= 0)
            {
                return BadRequest(new
                {
                    message =
                        "Mã cấu hình tỷ lệ không hợp lệ."
                });
            }

            var adminUserId =
                GetCurrentUserId();

            var result =
                await _service
                    .DeactivateRateConfigAsync(
                        rateConfigId,
                        request,
                        adminUserId);

            return Ok(new
            {
                message =
                    "Đã ngừng sử dụng cấu hình tỷ lệ BHXH.",

                data = result
            });
        }
        catch (Exception ex)
        {
            return HandleException(
                ex,
                nameof(DeactivateRateConfig));
        }
    }


    // ========================================================
    // 3. HỒ SƠ BHXH - ADMIN
    // ========================================================

    /// <summary>
    /// Admin lấy toàn bộ hồ sơ BHXH.
    ///
    /// GET:
    /// /api/SocialInsurance/profiles
    /// </summary>
    [HttpGet("profiles")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult>
        GetAllProfiles()
    {
        try
        {
            var result =
                await _service
                    .GetAllProfilesAsync();

            return Ok(result);
        }
        catch (Exception ex)
        {
            return HandleException(
                ex,
                nameof(GetAllProfiles));
        }
    }

    /// <summary>
    /// Admin lấy chi tiết hồ sơ theo ID hồ sơ.
    ///
    /// GET:
    /// /api/SocialInsurance/profiles/{profileId}
    /// </summary>
    [HttpGet("profiles/{profileId:int}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult>
        GetProfileById(int profileId)
    {
        try
        {
            if (profileId <= 0)
            {
                return BadRequest(new
                {
                    message =
                        "Mã hồ sơ BHXH không hợp lệ."
                });
            }

            var result =
                await _service
                    .GetProfileByIdAsync(profileId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return HandleException(
                ex,
                nameof(GetProfileById));
        }
    }

    /// <summary>
    /// Admin lấy hồ sơ theo ID nhân viên.
    ///
    /// GET:
    /// /api/SocialInsurance/profiles/user/{userId}
    /// </summary>
    [HttpGet("profiles/user/{userId:int}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult>
        GetProfileByUserIdForAdmin(int userId)
    {
        try
        {
            if (userId <= 0)
            {
                return BadRequest(new
                {
                    message =
                        "Mã nhân viên không hợp lệ."
                });
            }

            var result =
                await _service
                    .GetProfileByUserIdAsync(userId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return HandleException(
                ex,
                nameof(GetProfileByUserIdForAdmin));
        }
    }

    /// <summary>
    /// Admin tạo hồ sơ BHXH cho nhân viên FULL_TIME.
    ///
    /// POST:
    /// /api/SocialInsurance/profiles
    ///
    /// Hồ sơ mới có trạng thái PENDING.
    /// </summary>
    [HttpPost("profiles")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult>
        CreateProfile(
            [FromBody]
            CreateBhxhEmployeeProfileRequest request)
    {
        try
        {
            var adminUserId =
                GetCurrentUserId();

            var result =
                await _service
                    .CreateProfileAsync(
                        request,
                        adminUserId);

            return Ok(new
            {
                message =
                    "Đã tạo hồ sơ BHXH thành công.",

                data = result
            });
        }
        catch (Exception ex)
        {
            return HandleException(
                ex,
                nameof(CreateProfile));
        }
    }

    /// <summary>
    /// Admin cập nhật thông tin hồ sơ BHXH.
    ///
    /// PUT:
    /// /api/SocialInsurance/profiles/{profileId}
    ///
    /// Không cho đổi nhân viên sở hữu hồ sơ.
    /// </summary>
    [HttpPut("profiles/{profileId:int}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult>
        UpdateProfile(
            int profileId,
            [FromBody]
            UpdateBhxhEmployeeProfileRequest request)
    {
        try
        {
            if (profileId <= 0)
            {
                return BadRequest(new
                {
                    message =
                        "Mã hồ sơ BHXH không hợp lệ."
                });
            }

            var adminUserId =
                GetCurrentUserId();

            var result =
                await _service
                    .UpdateProfileAsync(
                        profileId,
                        request,
                        adminUserId);

            return Ok(new
            {
                message =
                    "Đã cập nhật hồ sơ BHXH thành công.",

                data = result
            });
        }
        catch (Exception ex)
        {
            return HandleException(
                ex,
                nameof(UpdateProfile));
        }
    }

    /// <summary>
    /// Admin đổi trạng thái hồ sơ.
    ///
    /// PUT:
    /// /api/SocialInsurance/profiles/{profileId}/status
    ///
    /// Trạng thái:
    /// - PENDING
    /// - ACTIVE
    /// - SUSPENDED
    /// - STOPPED
    ///
    /// Đây là cơ chế ngừng hồ sơ theo nghiệp vụ,
    /// không xóa cứng dữ liệu.
    /// </summary>
    [HttpPut("profiles/{profileId:int}/status")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult>
        UpdateProfileStatus(
            int profileId,
            [FromBody]
            UpdateBhxhProfileStatusRequest request)
    {
        try
        {
            if (profileId <= 0)
            {
                return BadRequest(new
                {
                    message =
                        "Mã hồ sơ BHXH không hợp lệ."
                });
            }

            var adminUserId =
                GetCurrentUserId();

            var result =
                await _service
                    .UpdateProfileStatusAsync(
                        profileId,
                        request,
                        adminUserId);

            return Ok(new
            {
                message =
                    "Đã cập nhật trạng thái hồ sơ BHXH.",

                data = result
            });
        }
        catch (Exception ex)
        {
            return HandleException(
                ex,
                nameof(UpdateProfileStatus));
        }
    }


    // ========================================================
    // 4. HỒ SƠ CỦA CHÍNH STAFF
    // ========================================================

    /// <summary>
    /// Staff xem hồ sơ BHXH của chính mình.
    ///
    /// GET:
    /// /api/SocialInsurance/my-profile
    ///
    /// UserId được lấy từ JWT,
    /// không nhận từ đường dẫn hoặc Frontend.
    /// </summary>
    [HttpGet("my-profile")]
    [Authorize(Roles = "STAFF")]
    public async Task<IActionResult>
        GetMyProfile()
    {
        try
        {
            var currentUserId =
                GetCurrentUserId();

            var result =
                await _service
                    .GetProfileByUserIdAsync(
                        currentUserId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return HandleException(
                ex,
                nameof(GetMyProfile));
        }
    }

    // Staff xác nhận hoặc yêu cầu Admin chỉnh sửa
    // hồ sơ BHXH của chính mình.
    //
    // PUT:
    // /api/SocialInsurance/my-profile/confirmation
    //
    // UserId được lấy từ JWT.
    // Frontend không được truyền UserId.
    [HttpPut("my-profile/confirmation")]
    [Authorize(Roles = "STAFF")]
    public async Task<IActionResult>
        UpdateMyProfileConfirmation(
            [FromBody]
            UpdateMyBhxhConfirmationRequest request)
    {
        try
        {
            // Luôn lấy ID Staff từ JWT
            // để tránh xác nhận hộ tài khoản khác.
            var staffUserId =
                GetCurrentUserId();

            var result =
                await _service
                    .UpdateMyProfileConfirmationAsync(
                        staffUserId,
                        request);

            var message =
                result.StaffConfirmationStatus ==
                    "CONFIRMED"
                    ? "Bạn đã xác nhận thông tin hồ sơ BHXH."
                    : "Đã gửi yêu cầu chỉnh sửa hồ sơ BHXH đến Admin.";

            return Ok(new
            {
                message,
                data = result
            });
        }
        catch (Exception ex)
        {
            return HandleException(
                ex,
                nameof(UpdateMyProfileConfirmation));
        }
    }

    // ========================================================
    // 5. KHOẢN ĐÓNG BHXH - ADMIN
    // ========================================================

    /// <summary>
    /// Admin sinh các khoản đóng BHXH cho một tháng.
    ///
    /// POST:
    /// /api/SocialInsurance/contributions/generate
    ///
    /// Chỉ hồ sơ ACTIVE và nhân viên FULL_TIME
    /// mới được tạo khoản đóng.
    ///
    /// Bản ghi mới có trạng thái DRAFT.
    /// </summary>
    [HttpPost("contributions/generate")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult>
        GenerateContributions(
            [FromBody]
            GenerateBhxhContributionsRequest request)
    {
        try
        {
            var result =
                await _service
                    .GenerateContributionsAsync(
                        request);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return HandleException(
                ex,
                nameof(GenerateContributions));
        }
    }

    /// <summary>
    /// Admin lấy danh sách khoản đóng theo tháng và năm.
    ///
    /// GET:
    /// /api/SocialInsurance/contributions?month=8&amp;year=2026
    /// </summary>
    [HttpGet("contributions")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult>
        GetContributionsByPeriod(
            [FromQuery] int month,
            [FromQuery] int year)
    {
        try
        {
            var result =
                await _service
                    .GetContributionsByPeriodAsync(
                        month,
                        year);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return HandleException(
                ex,
                nameof(GetContributionsByPeriod));
        }
    }

    /// <summary>
    /// Admin lấy chi tiết một khoản đóng.
    ///
    /// GET:
    /// /api/SocialInsurance/contributions/{id}
    /// </summary>
    [HttpGet("contributions/{contributionId:int}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult>
        GetContributionById(int contributionId)
    {
        try
        {
            if (contributionId <= 0)
            {
                return BadRequest(new
                {
                    message =
                        "Mã khoản đóng BHXH không hợp lệ."
                });
            }

            var result =
                await _service
                    .GetContributionByIdAsync(
                        contributionId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return HandleException(
                ex,
                nameof(GetContributionById));
        }
    }

    /// <summary>
    /// Admin xem lịch sử đóng của một nhân viên.
    ///
    /// GET:
    /// /api/SocialInsurance/contributions/user/{userId}
    /// </summary>
    [HttpGet("contributions/user/{userId:int}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult>
        GetContributionsByUserForAdmin(int userId)
    {
        try
        {
            if (userId <= 0)
            {
                return BadRequest(new
                {
                    message =
                        "Mã nhân viên không hợp lệ."
                });
            }

            var result =
                await _service
                    .GetContributionsByUserIdAsync(
                        userId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return HandleException(
                ex,
                nameof(GetContributionsByUserForAdmin));
        }
    }

    /// <summary>
    /// Admin xác nhận khoản đóng.
    ///
    /// PUT:
    /// /api/SocialInsurance/contributions/{id}/confirm
    ///
    /// Chuyển trạng thái:
    /// DRAFT → CONFIRMED.
    /// </summary>
    [HttpPut(
        "contributions/{contributionId:int}/confirm")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult>
        ConfirmContribution(int contributionId)
    {
        try
        {
            if (contributionId <= 0)
            {
                return BadRequest(new
                {
                    message =
                        "Mã khoản đóng BHXH không hợp lệ."
                });
            }

            var adminUserId =
                GetCurrentUserId();

            var result =
                await _service
                    .ConfirmContributionAsync(
                        contributionId,
                        adminUserId);

            return Ok(new
            {
                message =
                    "Đã xác nhận khoản đóng BHXH.",

                data = result
            });
        }
        catch (Exception ex)
        {
            return HandleException(
                ex,
                nameof(ConfirmContribution));
        }
    }

    /// <summary>
    /// Admin đánh dấu khoản đóng đã được nộp.
    ///
    /// PUT:
    /// /api/SocialInsurance/contributions/{id}/paid
    ///
    /// Chuyển trạng thái:
    /// CONFIRMED → PAID.
    /// </summary>
    [HttpPut(
        "contributions/{contributionId:int}/paid")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult>
        MarkContributionPaid(int contributionId)
    {
        try
        {
            if (contributionId <= 0)
            {
                return BadRequest(new
                {
                    message =
                        "Mã khoản đóng BHXH không hợp lệ."
                });
            }

            var adminUserId =
                GetCurrentUserId();

            var result =
                await _service
                    .MarkContributionPaidAsync(
                        contributionId,
                        adminUserId);

            return Ok(new
            {
                message =
                    "Đã đánh dấu khoản đóng BHXH là PAID.",

                data = result
            });
        }
        catch (Exception ex)
        {
            return HandleException(
                ex,
                nameof(MarkContributionPaid));
        }
    }

    /// <summary>
    /// Admin hủy một khoản đóng bị tạo sai.
    ///
    /// PUT:
    /// /api/SocialInsurance/contributions/{id}/cancel
    ///
    /// Không xóa bản ghi khỏi database.
    /// Trạng thái chuyển thành CANCELLED.
    ///
    /// Khoản đã PAID không được hủy.
    /// </summary>
    [HttpPut(
        "contributions/{contributionId:int}/cancel")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult>
        CancelContribution(
            int contributionId,
            [FromBody]
            CancelBhxhContributionRequest request)
    {
        try
        {
            if (contributionId <= 0)
            {
                return BadRequest(new
                {
                    message =
                        "Mã khoản đóng BHXH không hợp lệ."
                });
            }

            var adminUserId =
                GetCurrentUserId();

            var result =
                await _service
                    .CancelContributionAsync(
                        contributionId,
                        request,
                        adminUserId);

            return Ok(new
            {
                message =
                    "Đã hủy khoản đóng BHXH.",

                data = result
            });
        }
        catch (Exception ex)
        {
            return HandleException(
                ex,
                nameof(CancelContribution));
        }
    }


    // ========================================================
    // 6. LỊCH SỬ ĐÓNG CỦA CHÍNH STAFF
    // ========================================================

    /// <summary>
    /// Staff xem lịch sử đóng BHXH của chính mình.
    ///
    /// GET:
    /// /api/SocialInsurance/my-contributions
    ///
    /// UserId luôn lấy từ JWT.
    /// </summary>
    [HttpGet("my-contributions")]
    [Authorize(Roles = "STAFF")]
    public async Task<IActionResult>
        GetMyContributions()
    {
        try
        {
            var currentUserId =
                GetCurrentUserId();

            var result =
                await _service
                    .GetContributionsByUserIdAsync(
                        currentUserId);

            return Ok(result);
        }
        catch (Exception ex)
        {
            return HandleException(
                ex,
                nameof(GetMyContributions));
        }
    }
}
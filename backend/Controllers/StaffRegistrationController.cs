using System.Security.Claims;
using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LuanVanTotNghiep.Controllers;

/// <summary>
/// Controller chỉ phụ trách PHIẾU ĐĂNG KÝ CA.
///
/// Sau khi refactor, file này KHÔNG còn:
/// - Công bố lịch chính thức.
/// - Lấy lịch chính thức.
/// - Quét QR chấm công.
///
/// Ba nhóm trên được chuyển lần lượt sang:
/// - FinalScheduleController.
/// - AttendanceController.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StaffRegistrationController : ControllerBase
{
    private readonly StaffRegistrationService
        _registrationService;

    public StaffRegistrationController(
        StaffRegistrationService registrationService)
    {
        _registrationService = registrationService;
    }

    /// <summary>
    /// Lấy UserId từ JWT.
    ///
    /// Không dùng UserId do Frontend gửi lên để tránh
    /// người dùng giả mạo ID của tài khoản khác.
    /// </summary>
    private int GetCurrentUserId()
    {
        var actorIdValue =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!int.TryParse(
                actorIdValue,
                out var actorId) ||
            actorId <= 0)
        {
            throw new UnauthorizedAccessException(
                "Không xác định được người dùng hiện tại.");
        }

        return actorId;
    }

    /// <summary>
    /// Staff đăng ký một ca.
    ///
    /// Backend tự quyết định:
    /// - REGISTERED: còn vị trí chính thức.
    /// - WAITLIST: ca đã đủ vị trí chính thức.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Register(
        [FromBody] RegisterShiftDto dto)
    {
        try
        {
            // Luôn ghi đè UserId từ JWT.
            dto.UserId =
                GetCurrentUserId();

            var registration =
                await _registrationService
                    .RegisterAsync(dto);

            var isWaitlist =
                string.Equals(
                    registration.Status,
                    "WAITLIST",
                    StringComparison.OrdinalIgnoreCase);

            return Ok(new
            {
                message = isWaitlist
                    ? "Ca đã đủ vị trí chính thức. " +
                      "Bạn đã được thêm vào danh sách chờ."
                    : "Đăng ký ca làm thành công!",

                registrationId =
                    registration.Id,

                status =
                    registration.Status,

                registeredAt =
                    registration.RegisteredAt
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new
            {
                message = ex.Message
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    /// <summary>
    /// Lấy danh sách phiếu đăng ký của một đợt.
    ///
    /// Giữ nguyên quyền như code hiện tại để không làm
    /// thay đổi hành vi trong lúc refactor.
    /// </summary>
    [HttpGet("period/{periodId:int}")]
    public async Task<IActionResult> GetByPeriod(
        int periodId)
    {
        try
        {
            var list =
                await _registrationService
                    .GetRegistrationsByPeriodAsync(
                        periodId);

            return Ok(list);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                message = ex.Message
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    /// <summary>
    /// Manager/Admin hủy một phiếu đăng ký.
    ///
    /// Swagger gửi body dạng chuỗi JSON:
    /// "CANCELLED"
    /// </summary>
    [HttpPut("{id:int}/status")]
    [Authorize(Roles = "MANAGER,ADMIN")]
    public async Task<IActionResult> UpdateStatus(
        int id,
        [FromBody] string newStatus)
    {
        try
        {
            await _registrationService
                .UpdateStatusAsync(
                    id,
                    newStatus);

            return Ok(new
            {
                message =
                    "Đã hủy phiếu đăng ký."
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    /// <summary>
/// Staff xem các ca mình đã đăng ký trong một đợt.
///
/// Route:
/// GET /api/StaffRegistration/my-schedule/{userId}/{periodId}
///
/// Staff chỉ xem được dữ liệu của chính mình.
/// Manager/Admin có thể xem dữ liệu của người khác.
/// </summary>
[HttpGet(
    "my-schedule/{userId:int}/{periodId:int}")]
public async Task<IActionResult> GetMySchedule(
    int userId,
    int periodId)
{
    try
    {
        var actorId =
            GetCurrentUserId();

        var role =
            User.FindFirstValue(
                ClaimTypes.Role)?
                .Trim()
                .ToUpperInvariant();

        var isManagement =
            role == "MANAGER" ||
            role == "ADMIN";

        // Ngăn Staff truyền UserId của tài khoản khác.
        if (!isManagement &&
            actorId != userId)
        {
            return Forbid();
        }

        var schedule =
            await _registrationService
                .GetMyScheduleAsync(
                    userId,
                    periodId);

        return Ok(schedule);
    }
    catch (UnauthorizedAccessException ex)
    {
        return Unauthorized(new
        {
            message = ex.Message
        });
    }
    catch (KeyNotFoundException ex)
    {
        return NotFound(new
        {
            message = ex.Message
        });
    }
    catch (Exception ex)
    {
        return BadRequest(new
        {
            message = ex.Message
        });
    }
}

    /// <summary>
    /// Staff tự hủy phiếu REGISTERED hoặc WAITLIST.
    ///
    /// Backend kiểm tra UserId trong URL phải trùng
    /// với UserId trong JWT.
    /// </summary>
    [HttpDelete("{id:int}/user/{userId:int}")]
    public async Task<IActionResult>
        CancelRegistration(
            int id,
            int userId)
    {
        try
        {
            var actorId =
                GetCurrentUserId();

            if (actorId != userId)
            {
                return Forbid();
            }

            var promotedRegistration =
                await _registrationService
                    .CancelRegistrationAsync(
                        id,
                        actorId);

            return Ok(new
            {
                message =
                    "Hủy ca thành công.",

                waitlistPromoted =
                    promotedRegistration != null,

                promotedRegistrationId =
                    promotedRegistration?.Id
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new
            {
                message = ex.Message
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }
}
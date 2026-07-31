using System.Security.Claims;
using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LuanVanTotNghiep.Controllers;

/// <summary>
/// Controller phụ trách toàn bộ nghiệp vụ LỊCH CHÍNH THỨC:
///
/// 1. Công bố lịch.
/// 2. Lấy lịch đã công bố.
/// 3. Ghi nhận nghỉ có phép.
/// 4. Ghi nhận vắng không phép.
/// 5. Lấy WAITLIST phù hợp.
/// 6. Chọn người thay khẩn cấp.
///
/// Không xử lý đăng ký ca và không xử lý QR chấm công.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FinalScheduleController : ControllerBase
{
    private readonly FinalScheduleService
        _finalScheduleService;

    private readonly EmergencyReplacementService
        _replacementService;

    public FinalScheduleController(
        FinalScheduleService finalScheduleService,
        EmergencyReplacementService replacementService)
    {
        _finalScheduleService =
            finalScheduleService;

        _replacementService =
            replacementService;
    }

    /// <summary>
    /// Lấy UserId người đang đăng nhập từ JWT.
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

    // ============================================================
    // PHẦN 1: CÔNG BỐ VÀ ĐỌC LỊCH CHÍNH THỨC
    // ============================================================

    /// <summary>
    /// Manager công bố lịch chính thức.
    ///
    /// Route mới:
    /// POST /api/FinalSchedule/publish
    ///
    /// Route cũ vẫn được giữ tạm để các màn hình chưa sửa
    /// không bị lỗi:
    /// POST /api/StaffRegistration/publish
    /// </summary>
    [HttpPost("publish")]
    [HttpPost("~/api/StaffRegistration/publish")]
    [Authorize(Roles = "MANAGER")]
    public async Task<IActionResult> PublishSchedule(
        [FromBody] PublishScheduleDto dto)
    {
        try
        {
            await _finalScheduleService
                .PublishScheduleAsync(dto);

            return Ok(new
            {
                message =
                    "Đã công bố lịch làm việc thành công!"
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
    /// Lấy lịch chính thức của một đợt.
    ///
    /// Route mới:
    /// GET /api/FinalSchedule/period/{periodId}
    ///
    /// Route cũ được giữ tạm:
    /// GET /api/StaffRegistration/final-schedule/period/{periodId}
    /// </summary>
    [HttpGet("period/{periodId:int}")]
    [HttpGet(
        "~/api/StaffRegistration/final-schedule/period/{periodId:int}")]
    public async Task<IActionResult>
        GetFinalScheduleByPeriod(
            int periodId)
    {
        try
        {
            var result =
                await _finalScheduleService
                    .GetFinalSchedulesByPeriodAsync(
                        periodId);

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    // ============================================================
    // PHẦN 2: NGHỈ, VẮNG VÀ THAY CA KHẨN CẤP
    // ============================================================

    /// <summary>
    /// Manager ghi nhận Staff nghỉ có phép.
    /// </summary>
    [HttpPut(
        "{scheduleId:int}/approved-leave")]
    [Authorize(Roles = "MANAGER")]
    public async Task<IActionResult>
        MarkApprovedLeave(
            int scheduleId,
            [FromBody] ScheduleAbsenceDto dto)
    {
        try
        {
            var result =
                await _replacementService
                    .MarkApprovedLeaveAsync(
                        scheduleId,
                        GetCurrentUserId(),
                        dto);

            return Ok(new
            {
                message =
                    "Đã ghi nhận nhân viên nghỉ có phép.",

                data = result
            });
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
    /// Manager ghi nhận Staff vắng không phép.
    ///
    /// Service kiểm tra mốc giờ bắt đầu ca cộng
    /// thời gian chờ cho phép.
    /// </summary>
    [HttpPut(
        "{scheduleId:int}/absent")]
    [Authorize(Roles = "MANAGER")]
    public async Task<IActionResult>
        MarkAbsent(
            int scheduleId,
            [FromBody] ScheduleAbsenceDto dto)
    {
        try
        {
            var result =
                await _replacementService
                    .MarkAbsentAsync(
                        scheduleId,
                        GetCurrentUserId(),
                        dto);

            return Ok(new
            {
                message =
                    "Đã ghi nhận nhân viên vắng không phép.",

                data = result
            });
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
    /// Lấy các Staff WAITLIST phù hợp để Manager liên hệ.
    /// </summary>
    [HttpGet(
        "{scheduleId:int}/replacement-candidates")]
    [Authorize(Roles = "MANAGER")]
    public async Task<IActionResult>
        GetReplacementCandidates(
            int scheduleId)
    {
        try
        {
            var result =
                await _replacementService
                    .GetReplacementCandidatesAsync(
                        scheduleId,
                        GetCurrentUserId());

            return Ok(result);
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
    /// Manager xác nhận Staff WAITLIST đã đồng ý thay ca.
    /// </summary>
    [HttpPost(
        "{scheduleId:int}/emergency-replacement")]
    [Authorize(Roles = "MANAGER")]
    public async Task<IActionResult>
        AssignEmergencyReplacement(
            int scheduleId,
            [FromBody] EmergencyReplacementDto dto)
    {
        try
        {
            var result =
                await _replacementService
                    .AssignEmergencyReplacementAsync(
                        scheduleId,
                        GetCurrentUserId(),
                        dto);

            return Ok(new
            {
                message =
                    "Đã điều động nhân viên thay ca thành công.",

                data = result
            });
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
}
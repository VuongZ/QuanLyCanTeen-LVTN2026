using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LuanVanTotNghiep.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class KhoExportController : ControllerBase
    {
        private readonly KhoExportService _exportService;

        public KhoExportController(KhoExportService exportService)
        {
            _exportService = exportService;
        }

        [HttpGet("available-schedules")]
        public async Task<IActionResult> GetAvailableSchedules([FromQuery] int? managerId)
        {
            try
            {
                var tokenUserId = GetIntClaim(ClaimTypes.NameIdentifier, "UserId", "userId", "id", "Id");
                var finalManagerId = tokenUserId ?? managerId ?? 0;

                if (finalManagerId <= 0)
                {
                    return Unauthorized(new { message = "Không tìm thấy thông tin người dùng trong token." });
                }

                var data = await _exportService.GetTodayExportSchedulesAsync(finalManagerId);
                return Ok(data);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi lấy ca xuất hàng: " + ex.Message });
            }
        }

        [HttpPost("submit-export")]
        public async Task<IActionResult> SubmitExportTicket([FromBody] CreateExportTicketDto dto)
        {
            if (dto.Items == null || dto.Items.Count == 0)
            {
                return BadRequest(new { message = "Phiếu xuất không có sản phẩm nào." });
            }

            try
            {
                var tokenUserId = GetIntClaim(ClaimTypes.NameIdentifier, "UserId", "userId", "id", "Id");
                var tokenBranchId = GetIntClaim("BranchId", "branchId", "branch_id");

                if (tokenUserId.HasValue)
                {
                    dto.ManagerId = tokenUserId.Value;
                }

                if (tokenBranchId.HasValue)
                {
                    dto.BranchId = tokenBranchId.Value;
                }

                var ticketId = await _exportService.CreateExportTicketAsync(dto);

                return Ok(new
                {
                    message = "Xuất hàng ra quầy thành công!",
                    exportTicketId = ticketId
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi xuất kho: " + ex.Message });
            }
        }

        private int? GetIntClaim(params string[] claimTypes)
        {
            foreach (var claimType in claimTypes)
            {
                var value = User.Claims.FirstOrDefault(c => c.Type == claimType)?.Value;

                if (int.TryParse(value, out var result))
                {
                    return result;
                }
            }

            return null;
        }

       [HttpGet("front-stock-tickets")]
public async Task<IActionResult> GetFrontStockExportTickets([FromQuery] int? branchId)
{
    try
    {
        var finalBranchId = ResolveBranchIdForQuery(branchId);

        if (finalBranchId == -1)
            return Unauthorized(new { message = "Không tìm thấy thông tin chi nhánh trong token." });

        var data = await _exportService.GetFrontStockExportTicketsAsync(
            finalBranchId == 0 ? null : finalBranchId
        );

        return Ok(data);
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { message = "Lỗi hệ thống khi lấy phiếu xuất ra quầy: " + ex.Message });
    }
}

[HttpGet("front-stock-tickets/{id}")]
public async Task<IActionResult> GetFrontStockExportTicketDetail(int id, [FromQuery] int? branchId)
{
    try
    {
        var finalBranchId = ResolveBranchIdForQuery(branchId);

        if (finalBranchId == -1)
            return Unauthorized(new { message = "Không tìm thấy thông tin chi nhánh trong token." });

        var data = await _exportService.GetFrontStockExportTicketDetailAsync(
            id,
            finalBranchId == 0 ? null : finalBranchId
        );

        if (data == null)
            return NotFound(new { message = "Không tìm thấy phiếu xuất ra quầy." });

        return Ok(data);
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { message = "Lỗi hệ thống khi lấy chi tiết phiếu xuất ra quầy: " + ex.Message });
    }
}

private int ResolveBranchIdForQuery(int? requestedBranchId)
{
    var role = GetClaimValue(ClaimTypes.Role, "role", "Role")?.ToUpperInvariant();
    var isAdmin = role == "ADMIN" || role == "QUẢN TRỊ" || role == "QUAN TRI";

    if (isAdmin)
    {
        return requestedBranchId.HasValue && requestedBranchId.Value > 0
            ? requestedBranchId.Value
            : 0;
    }

    var tokenBranchIdStr = GetClaimValue("BranchId", "branchId", "branch_id");

    if (!int.TryParse(tokenBranchIdStr, out var tokenBranchId) || tokenBranchId <= 0)
        return -1;

    return tokenBranchId;
}

private string? GetClaimValue(params string[] claimTypes)
{
    foreach (var claimType in claimTypes)
    {
        var value = User.Claims.FirstOrDefault(c => c.Type == claimType)?.Value;

        if (!string.IsNullOrWhiteSpace(value))
            return value;
    }

    return null;
}
    }
}
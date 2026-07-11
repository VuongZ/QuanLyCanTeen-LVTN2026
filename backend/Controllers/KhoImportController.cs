using Microsoft.AspNetCore.Mvc;
using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Services;
using System;
using System.Threading.Tasks;
using System.Security.Claims;

namespace LuanVanTotNghiep.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class KhoImportController : ControllerBase
    {
        private readonly KhoImportService _importService;

        public KhoImportController(KhoImportService importService)
        {
            _importService = importService;
        }

        [HttpPost("submit-import")]
        public async Task<IActionResult> SubmitImportTicket([FromBody] CreateImportTicketDto dto)
        {
            if (dto.Items == null || dto.Items.Count == 0)
            {
                return BadRequest(new { message = "Phiếu nhập từ file Excel không có sản phẩm nào hợp lệ." });
            }

            try
            {
                await _importService.CreateImportTicketAsync(dto);
                return Ok(new { message = "Nhập kho thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

       [HttpGet("inventory-tickets")]
public async Task<IActionResult> GetInventoryImportTickets([FromQuery] int? branchId)
{
    try
    {
        var finalBranchId = ResolveBranchIdForQuery(branchId);

        if (finalBranchId == -1)
            return Unauthorized(new { message = "Không tìm thấy thông tin chi nhánh trong token." });

        var data = await _importService.GetInventoryImportTicketsAsync(
            finalBranchId == 0 ? null : finalBranchId
        );

        return Ok(data);
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { message = "Lỗi hệ thống khi lấy phiếu nhập kho: " + ex.Message });
    }
}

[HttpGet("inventory-tickets/{id}")]
public async Task<IActionResult> GetInventoryImportTicketDetail(int id, [FromQuery] int? branchId)
{
    try
    {
        var finalBranchId = ResolveBranchIdForQuery(branchId);

        if (finalBranchId == -1)
            return Unauthorized(new { message = "Không tìm thấy thông tin chi nhánh trong token." });

        var data = await _importService.GetInventoryImportTicketDetailAsync(
            id,
            finalBranchId == 0 ? null : finalBranchId
        );

        if (data == null)
            return NotFound(new { message = "Không tìm thấy phiếu nhập kho." });

        return Ok(data);
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { message = "Lỗi hệ thống khi lấy chi tiết phiếu nhập kho: " + ex.Message });
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
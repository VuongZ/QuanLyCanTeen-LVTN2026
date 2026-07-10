using LuanVanTotNghiep.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LuanVanTotNghiep.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FrontStockController : ControllerBase
    {
        private readonly FrontStockRepo _frontStockRepo;

        public FrontStockController(FrontStockRepo frontStockRepo)
        {
            _frontStockRepo = frontStockRepo;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] int? branchId)
        {
            try
            {
                var role = GetClaimValue(ClaimTypes.Role, "role", "Role")?.ToUpperInvariant();
                var isAdmin = role == "ADMIN" || role == "QUẢN TRỊ" || role == "QUAN TRI";

                if (!isAdmin)
                {
                    var tokenBranchIdStr = GetClaimValue("BranchId", "branchId", "branch_id");

                    if (!int.TryParse(tokenBranchIdStr, out var tokenBranchId) || tokenBranchId <= 0)
                    {
                        return Unauthorized(new { message = "Không tìm thấy thông tin chi nhánh trong token." });
                    }

                    var branchData = await _frontStockRepo.GetFrontStockByBranchIdAsync(tokenBranchId);
                    return Ok(branchData);
                }

                if (branchId.HasValue && branchId.Value > 0)
                {
                    var branchData = await _frontStockRepo.GetFrontStockByBranchIdAsync(branchId.Value);
                    return Ok(branchData);
                }

                var allData = await _frontStockRepo.GetAllFrontStockAsync();
                return Ok(allData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        private string? GetClaimValue(params string[] claimTypes)
        {
            foreach (var claimType in claimTypes)
            {
                var value = User.Claims.FirstOrDefault(c => c.Type == claimType)?.Value;

                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return null;
        }
    }
}
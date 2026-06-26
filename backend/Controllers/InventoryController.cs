using LuanVanTotNghiep.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LuanVanTotNghiep.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // 👈 Khóa bảo mật
    public class InventoryController : ControllerBase
    {
        private readonly InventoryRepo _inventoryRepo;

        public InventoryController(InventoryRepo inventoryRepo)
        {
            _inventoryRepo = inventoryRepo;
        }

        // Lấy báo cáo tồn kho
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] int? branchId) // 👈 Nhận tham số branchId từ React
        {
            try
            {
                // 1. Đọc quyền từ Token
                var userRole = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value?.ToUpper();

                // 2. Logic dành cho MANAGER / STAFF
                if (userRole != "ADMIN")
                {
                    // Tự động ép lấy BranchId từ trong Token, không cho phép truyền branchId tùy tiện
                    var tokenBranchIdStr = User.Claims.FirstOrDefault(c => c.Type == "BranchId")?.Value;
                    if (int.TryParse(tokenBranchIdStr, out int tokenBranchId))
                    {
                        var data = await _inventoryRepo.GetInventoryByBranchIdAsync(tokenBranchId);
                        return Ok(data);
                    }
                    return Unauthorized(new { message = "Lỗi phân quyền chi nhánh!" });
                }

                // 3. Logic dành cho ADMIN
                if (branchId.HasValue && branchId.Value > 0)
                {
                    // Trạng thái 3a: Admin có chọn dropdown để lọc theo cơ sở cụ thể
                    var data = await _inventoryRepo.GetInventoryByBranchIdAsync(branchId.Value);
                    return Ok(data);
                }
                else
                {
                    // Trạng thái 3b: Admin chọn "Tất cả cơ sở" (branchId rỗng)
                    var data = await _inventoryRepo.GetAllInventoryAsync();
                    return Ok(data);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống: " + ex.Message });
            }
        }
    }
}
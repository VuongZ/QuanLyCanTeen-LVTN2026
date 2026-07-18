using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LuanVanTotNghiep.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SupplierController : ControllerBase
    {
        private readonly SupplierService _service;

        public SupplierController(SupplierService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetSuppliers() => Ok(await _service.GetAllSuppliersAsync());

        [HttpGet("deleted")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> GetDeletedSuppliers() => Ok(await _service.GetDeletedSuppliersAsync());

        [HttpPost]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> CreateSupplier([FromBody] CreateUpdateSupplierDto supplier)
        {
            var created = await _service.CreateSupplierAsync(supplier);
            return Ok(created);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> UpdateSupplier(int id, [FromBody] CreateUpdateSupplierDto supplier)
        {
            try
            {
                await _service.UpdateSupplierAsync(id, supplier);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Không tìm thấy nhà cung cấp." });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> DeleteSupplier(int id)
        {
            var deleted = await _service.DeleteSupplierAsync(id);
            if (!deleted)
                return NotFound(new { message = "Không tìm thấy nhà cung cấp." });

            return NoContent();
        }

        [HttpPatch("{id}/restore")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> RestoreSupplier(int id)
        {
            var restored = await _service.RestoreSupplierAsync(id);
            if (!restored)
                return NotFound(new { message = "Không tìm thấy nhà cung cấp đã xóa." });

            return Ok(new { message = "Khôi phục nhà cung cấp thành công." });
        }
    }
}

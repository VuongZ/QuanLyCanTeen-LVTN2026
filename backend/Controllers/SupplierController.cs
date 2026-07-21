using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LuanVanTotNghiep.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SupplierController : ControllerBase
    {
        private readonly SupplierService _service;

        public SupplierController(SupplierService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetSuppliers()
        {
            var suppliers =
                await _service.GetAllSuppliersAsync();

            return Ok(suppliers);
        }

        [HttpGet("deleted")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> GetDeletedSuppliers()
        {
            var suppliers =
                await _service.GetDeletedSuppliersAsync();

            return Ok(suppliers);
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> CreateSupplier(
            [FromBody] CreateUpdateSupplierDto dto
        )
        {
            try
            {
                var created =
                    await _service.CreateSupplierAsync(dto);

                return Ok(created);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> UpdateSupplier(
            int id,
            [FromBody] CreateUpdateSupplierDto dto
        )
        {
            try
            {
                var updated =
                    await _service.UpdateSupplierAsync(id, dto);

                return Ok(updated);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    message = ex.Message
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> DeleteSupplier(int id)
        {
            var deleted =
                await _service.DeleteSupplierAsync(id);

            if (!deleted)
            {
                return NotFound(new
                {
                    message =
                        "Không tìm thấy nhà phân phối đang hoạt động."
                });
            }

            return Ok(new
            {
                message =
                    "Đã ngừng hoạt động nhà phân phối."
            });
        }

        [HttpPatch("{id:int}/restore")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> RestoreSupplier(int id)
        {
            try
            {
                var restored =
                    await _service.RestoreSupplierAsync(id);

                if (!restored)
                {
                    return NotFound(new
                    {
                        message =
                            "Không tìm thấy nhà phân phối đã ngừng hoạt động."
                    });
                }

                return Ok(new
                {
                    message =
                        "Khôi phục nhà phân phối thành công."
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new
                {
                    message = ex.Message
                });
            }
        }
    }
}
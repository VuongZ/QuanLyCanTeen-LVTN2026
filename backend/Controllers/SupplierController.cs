using Microsoft.AspNetCore.Mvc;
using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.Repositories;
using Microsoft.AspNetCore.Authorization; // Nhớ import thư viện Authorize

namespace LuanVanTotNghiep.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // Hoặc [Authorize] nếu bạn chỉ cho phép người dùng đăng nhập nói chung được Xem
    public class SupplierController : ControllerBase
    {
        private readonly SupplierRepo _repo;
        public SupplierController(SupplierRepo repo) { _repo = repo; }

        // Mọi user đăng nhập (bao gồm cả Manager) đều có quyền XEM danh sách
        [HttpGet]
        public async Task<IActionResult> GetSuppliers() => Ok(await _repo.GetAllAsync());

        // 👉 CHỈ CÓ ADMIN MỚI ĐƯỢC CRUD (Thêm, Sửa, Xóa)
        [HttpPost]
        [Authorize(Roles = "ADMIN")] 
        public async Task<IActionResult> CreateSupplier([FromBody] KhoSupplier supplier)
        {
            await _repo.AddAsync(supplier);
            return Ok(supplier);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> UpdateSupplier(int id, [FromBody] KhoSupplier supplier)
        {
            if (id != supplier.Id) return BadRequest();
            await _repo.UpdateAsync(supplier);
            return Ok(supplier);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> DeleteSupplier(int id)
        {
            await _repo.DeleteAsync(id);
            return NoContent();
        }
    }
}
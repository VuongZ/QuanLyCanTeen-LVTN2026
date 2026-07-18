using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Repositories;

namespace LuanVanTotNghiep.Services
{
    public class SupplierService
    {
        private readonly SupplierRepo _repo;

        public SupplierService(SupplierRepo repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<SupplierDto>> GetAllSuppliersAsync()
        {
            var suppliers = await _repo.GetAllAsync();
            // Map từ Entity sang DTO để trả cho Frontend
            return suppliers.Select(s => new SupplierDto
            {
                Id = s.Id,
                SupplierName = s.SupplierName,
                Phone = s.Phone,
                Address = s.Address,
                IsDeleted = s.IsDeleted == true,
                DeletedAt = s.DeletedAt
            });
        }

        public async Task<IEnumerable<SupplierDto>> GetDeletedSuppliersAsync()
        {
            var suppliers = await _repo.GetDeletedAsync();
            return suppliers.Select(s => new SupplierDto
            {
                Id = s.Id,
                SupplierName = s.SupplierName,
                Phone = s.Phone,
                Address = s.Address,
                IsDeleted = true,
                DeletedAt = s.DeletedAt
            });
        }

        public async Task<SupplierDto> CreateSupplierAsync(CreateUpdateSupplierDto dto)
        {
            // Có thể thêm logic kiểm tra trùng tên ở đây nếu cần

            var supplier = new KhoSupplier
            {
                SupplierName = dto.SupplierName,
                Phone = dto.Phone,
                Address = dto.Address
            };

            await _repo.AddAsync(supplier);

            return new SupplierDto
            {
                Id = supplier.Id,
                SupplierName = supplier.SupplierName,
                Phone = supplier.Phone,
                Address = supplier.Address
            };
        }

        public async Task UpdateSupplierAsync(int id, CreateUpdateSupplierDto dto)
        {
            var existingSupplier = await _repo.GetByIdAsync(id);
            if (existingSupplier == null) throw new KeyNotFoundException("Không tìm thấy nhà cung cấp");

            existingSupplier.SupplierName = dto.SupplierName;
            existingSupplier.Phone = dto.Phone;
            existingSupplier.Address = dto.Address;

            await _repo.UpdateAsync(existingSupplier);
        }

        public async Task<bool> DeleteSupplierAsync(int id)
        {
            return await _repo.SoftDeleteAsync(id);
        }

        public async Task<bool> RestoreSupplierAsync(int id)
        {
            return await _repo.RestoreAsync(id);
        }
    }
}

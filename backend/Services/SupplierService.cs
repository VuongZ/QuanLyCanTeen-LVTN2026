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

        public async Task<IEnumerable<SupplierDto>>
            GetAllSuppliersAsync()
        {
            var suppliers = await _repo.GetAllAsync();

            return suppliers.Select(ToDto);
        }

        public async Task<IEnumerable<SupplierDto>>
            GetDeletedSuppliersAsync()
        {
            var suppliers = await _repo.GetDeletedAsync();

            return suppliers.Select(ToDto);
        }

        public async Task<SupplierDto> CreateSupplierAsync(
            CreateUpdateSupplierDto dto
        )
        {
            var supplierName = NormalizeRequiredName(
                dto.SupplierName
            );

            var exists = await _repo.ExistsActiveByNameAsync(
                supplierName
            );

            if (exists)
            {
                throw new InvalidOperationException(
                    "Đã tồn tại nhà phân phối đang hoạt động có cùng tên."
                );
            }

            var supplier = new KhoSupplier
            {
                SupplierName = supplierName,
                Phone = NormalizeOptional(dto.Phone),
                Address = NormalizeOptional(dto.Address),
                IsDeleted = false,
                DeletedAt = null
            };

            await _repo.AddAsync(supplier);

            return ToDto(supplier);
        }

        public async Task<SupplierDto> UpdateSupplierAsync(
            int id,
            CreateUpdateSupplierDto dto
        )
        {
            var supplier = await _repo.GetActiveByIdAsync(id);

            if (supplier == null)
            {
                throw new KeyNotFoundException(
                    "Không tìm thấy nhà phân phối."
                );
            }

            var supplierName = NormalizeRequiredName(
                dto.SupplierName
            );

            var exists = await _repo.ExistsActiveByNameAsync(
                supplierName,
                id
            );

            if (exists)
            {
                throw new InvalidOperationException(
                    "Đã tồn tại nhà phân phối đang hoạt động có cùng tên."
                );
            }

            supplier.SupplierName = supplierName;
            supplier.Phone = NormalizeOptional(dto.Phone);
            supplier.Address = NormalizeOptional(dto.Address);

            await _repo.UpdateAsync();

            return ToDto(supplier);
        }

        public async Task<bool> DeleteSupplierAsync(int id)
        {
            return await _repo.SoftDeleteAsync(id);
        }

        public async Task<bool> RestoreSupplierAsync(int id)
        {
            var supplier = await _repo.GetDeletedByIdAsync(id);

            if (supplier == null)
            {
                return false;
            }

            var duplicatedName =
                await _repo.ExistsActiveByNameAsync(
                    supplier.SupplierName
                );

            if (duplicatedName)
            {
                throw new InvalidOperationException(
                    "Không thể khôi phục vì đã có nhà phân phối đang hoạt động cùng tên."
                );
            }

            await _repo.RestoreAsync(supplier);

            return true;
        }

        private static string NormalizeRequiredName(
            string? value
        )
        {
            var supplierName = value?.Trim();

            if (string.IsNullOrWhiteSpace(supplierName))
            {
                throw new ArgumentException(
                    "Vui lòng nhập tên nhà phân phối."
                );
            }

            return supplierName;
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }

        private static SupplierDto ToDto(KhoSupplier supplier)
        {
            return new SupplierDto
            {
                Id = supplier.Id,
                SupplierName = supplier.SupplierName,
                Phone = supplier.Phone,
                Address = supplier.Address,
                IsDeleted = supplier.IsDeleted == true,
                DeletedAt = supplier.DeletedAt
            };
        }
    }
}
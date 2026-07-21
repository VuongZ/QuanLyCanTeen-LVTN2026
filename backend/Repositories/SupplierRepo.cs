using LuanVanTotNghiep.backend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Repositories
{
    public class SupplierRepo
    {
        private readonly AppDbContext _context;

        public SupplierRepo(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<KhoSupplier>> GetAllAsync()
        {
            return await _context.KhoSuppliers
                .AsNoTracking()
                .Where(supplier => supplier.IsDeleted != true)
                .OrderBy(supplier => supplier.SupplierName)
                .ToListAsync();
        }

        public async Task<List<KhoSupplier>> GetDeletedAsync()
        {
            return await _context.KhoSuppliers
                .AsNoTracking()
                .Where(supplier => supplier.IsDeleted == true)
                .OrderByDescending(supplier => supplier.DeletedAt)
                .ThenBy(supplier => supplier.SupplierName)
                .ToListAsync();
        }

        public async Task<KhoSupplier?> GetActiveByIdAsync(int id)
        {
            return await _context.KhoSuppliers
                .FirstOrDefaultAsync(
                    supplier =>
                        supplier.Id == id &&
                        supplier.IsDeleted != true
                );
        }

        public async Task<KhoSupplier?> GetDeletedByIdAsync(int id)
        {
            return await _context.KhoSuppliers
                .FirstOrDefaultAsync(
                    supplier =>
                        supplier.Id == id &&
                        supplier.IsDeleted == true
                );
        }

        public async Task<bool> ExistsActiveByNameAsync(
            string supplierName,
            int? excludeId = null
        )
        {
            var query = _context.KhoSuppliers
                .AsNoTracking()
                .Where(
                    supplier =>
                        supplier.IsDeleted != true &&
                        supplier.SupplierName == supplierName
                );

            if (excludeId.HasValue)
            {
                query = query.Where(
                    supplier => supplier.Id != excludeId.Value
                );
            }

            return await query.AnyAsync();
        }

        public async Task AddAsync(KhoSupplier supplier)
        {
            await _context.KhoSuppliers.AddAsync(supplier);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<bool> SoftDeleteAsync(int id)
        {
            var supplier = await GetActiveByIdAsync(id);

            if (supplier == null)
            {
                return false;
            }

            supplier.IsDeleted = true;
            supplier.DeletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task RestoreAsync(KhoSupplier supplier)
        {
            supplier.IsDeleted = false;
            supplier.DeletedAt = null;

            await _context.SaveChangesAsync();
        }
    }
}
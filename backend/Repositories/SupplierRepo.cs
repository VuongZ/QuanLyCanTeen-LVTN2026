using LuanVanTotNghiep.backend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Repositories
{
    public class SupplierRepo
    {
        private readonly AppDbContext _context;
        public SupplierRepo(AppDbContext context) { _context = context; }

        public async Task<IEnumerable<KhoSupplier>> GetAllAsync() => await _context.KhoSuppliers
            .AsNoTracking()
            .Where(supplier => supplier.IsDeleted != true)
            .OrderBy(supplier => supplier.SupplierName)
            .ToListAsync();

        public async Task<IEnumerable<KhoSupplier>> GetDeletedAsync() => await _context.KhoSuppliers
            .AsNoTracking()
            .Where(supplier => supplier.IsDeleted == true)
            .OrderByDescending(supplier => supplier.DeletedAt)
            .ThenBy(supplier => supplier.SupplierName)
            .ToListAsync();
        
        public async Task<KhoSupplier?> GetByIdAsync(int id) => await _context.KhoSuppliers
            .FirstOrDefaultAsync(supplier => supplier.Id == id && supplier.IsDeleted != true);
        
        public async Task AddAsync(KhoSupplier supplier)
        {
            _context.KhoSuppliers.Add(supplier);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(KhoSupplier supplier)
        {
            _context.KhoSuppliers.Update(supplier);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> SoftDeleteAsync(int id)
        {
            var supplier = await _context.KhoSuppliers
                .FirstOrDefaultAsync(item => item.Id == id && item.IsDeleted != true);
            if (supplier == null)
                return false;

            supplier.IsDeleted = true;
            supplier.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RestoreAsync(int id)
        {
            var supplier = await _context.KhoSuppliers
                .FirstOrDefaultAsync(item => item.Id == id && item.IsDeleted == true);
            if (supplier == null)
                return false;

            supplier.IsDeleted = false;
            supplier.DeletedAt = null;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}

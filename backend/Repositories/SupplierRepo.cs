using LuanVanTotNghiep.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Repositories
{
    public class SupplierRepo
    {
        private readonly AppDbContext _context;
        public SupplierRepo(AppDbContext context) { _context = context; }

        public async Task<IEnumerable<KhoSupplier>> GetAllAsync() => await _context.KhoSuppliers.ToListAsync();
        
        public async Task<KhoSupplier?> GetByIdAsync(int id) => await _context.KhoSuppliers.FindAsync(id);
        
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

        public async Task DeleteAsync(int id)
        {
            var supplier = await _context.KhoSuppliers.FindAsync(id);
            if (supplier != null)
            {
                _context.KhoSuppliers.Remove(supplier);
                await _context.SaveChangesAsync();
            }
        }
    }
}
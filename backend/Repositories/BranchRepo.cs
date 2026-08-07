using LuanVanTotNghiep.backend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Repositories
{
    public class BranchRepo : Repository<DmBranch>
    {
        public BranchRepo(AppDbContext context) : base(context)
        {
        }

        public override async Task<DmBranch?> GetbyId(int id)
        {
            return await _dbSet.FirstOrDefaultAsync(branch => branch.Id == id);
        }

        public async Task<List<DmBranch>> GetAllBranchesAsync(bool includeInactive)
        {
            var query = _dbSet.AsNoTracking().AsQueryable();

            if (!includeInactive)
            {
                query = query.Where(branch => branch.IsActive);
            }

            return await query
                .OrderBy(branch => branch.Name)
                .ToListAsync();
        }

        public async Task<bool> IsActiveAsync(int branchId)
        {
            return await _dbSet
                .AsNoTracking()
                .AnyAsync(branch =>
                    branch.Id == branchId &&
                    branch.IsActive);
        }
    }
}

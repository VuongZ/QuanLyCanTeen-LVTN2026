using LuanVanTotNghiep.backend.Models.Entities;
using Microsoft.EntityFrameworkCore;
namespace LuanVanTotNghiep.Repositories
{
    public class BranchRepo : Repository<DmBranch>
    {    
        public BranchRepo(AppDbContext context) : base (context)
        {          
        }

        public override async Task<DmBranch?> GetbyId(int id)
        {
            return await _dbSet.FirstOrDefaultAsync(b=>b.Id ==id);
        }
    }
}
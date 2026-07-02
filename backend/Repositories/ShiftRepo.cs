using LuanVanTotNghiep.backend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Repositories;

public class ShiftRepo : Repository<CaShift>
{
    public ShiftRepo(AppDbContext context) :base(context){}

    public override async Task<CaShift?> GetbyId(int id)
    {
        return await _dbSet.Include(s=>s.Branch)
        .FirstOrDefaultAsync(s=>s.Id==id);
    }

    public async Task<IEnumerable<CaShift>> GetShiftByBranchId(int branchId)
    {
        return await _dbSet
        .Include(s=>s.Branch)
        .Where(s=>s.BranchId==branchId)
        .ToListAsync();
    }
}
    

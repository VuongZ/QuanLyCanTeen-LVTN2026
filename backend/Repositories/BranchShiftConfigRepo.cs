using LuanVanTotNghiep.backend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Repositories;

public class BranchShiftConfigRepo : Repository<CaBranchShiftConfig>
{
    public BranchShiftConfigRepo(AppDbContext context) : base(context)
    {
    }

    public override async Task<CaBranchShiftConfig?> GetbyId(int id)
    {
        return await _dbSet
            .Include(c => c.Shift)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<CaBranchShiftConfig?> GetConfigByShiftAsync(int shiftId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(c => c.ShiftId == shiftId);
    }
    
    public async Task<IEnumerable<CaBranchShiftConfig>> GetAllConfigsAsync()
    {
        return await _dbSet
            .Include(c => c.Shift)
            .ToListAsync();
    }
}
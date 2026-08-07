using LuanVanTotNghiep.backend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Repositories;

public class ShiftRepo : Repository<CaShift>
{
    public ShiftRepo(AppDbContext context) : base(context)
    {
    }

    public override async Task<CaShift?> GetbyId(int id)
    {
        return await _dbSet
            .Include(shift => shift.Branch)
            .FirstOrDefaultAsync(shift => shift.Id == id);
    }

    public async Task<List<CaShift>> GetAllShiftsAsync(bool includeInactive)
    {
        var query = _dbSet
            .AsNoTracking()
            .Include(shift => shift.Branch)
            .AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(shift =>
                shift.IsActive &&
                shift.Branch != null &&
                shift.Branch.IsActive);
        }

        return await query
            .OrderBy(shift => shift.BranchId)
            .ThenBy(shift => shift.StartTime)
            .ThenBy(shift => shift.Id)
            .ToListAsync();
    }

    public async Task<IEnumerable<CaShift>> GetShiftByBranchId(
        int branchId,
        bool includeInactive = false)
    {
        var query = _dbSet
            .AsNoTracking()
            .Include(shift => shift.Branch)
            .Where(shift => shift.BranchId == branchId);

        if (!includeInactive)
        {
            query = query.Where(shift =>
                shift.IsActive &&
                shift.Branch != null &&
                shift.Branch.IsActive);
        }

        return await query
            .OrderBy(shift => shift.StartTime)
            .ThenBy(shift => shift.Id)
            .ToListAsync();
    }

    public async Task<bool> IsBranchActiveAsync(int branchId)
    {
        return await Context.DmBranches
            .AsNoTracking()
            .AnyAsync(branch =>
                branch.Id == branchId &&
                branch.IsActive);
    }
}

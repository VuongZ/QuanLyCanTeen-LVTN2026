using LuanVanTotNghiep.backend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Repositories;

public class SchedulePeriodRepo : Repository<CaSchedulePeriod>
{
    public SchedulePeriodRepo(AppDbContext context) : base(context)
    {
    }

    public override async Task<CaSchedulePeriod?> GetbyId(int id)
    {
        return await _dbSet
            .FirstOrDefaultAsync(sp => sp.Id == id);
    }

    public async Task<IEnumerable<CaSchedulePeriod>> GetOpenPeriodsAsync()
    {
        return await _dbSet
            .Where(sp => sp.Status == "OPEN")
            .ToListAsync();
    }
}
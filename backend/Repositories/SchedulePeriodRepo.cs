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

    public async Task<int> CloseExpiredOpenPeriodsAsync(
        DateOnly today,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(period =>
                period.Status == "OPEN" &&
                period.StartDate <= today)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    period => period.Status,
                    "CLOSED"),
                cancellationToken);
    }
}
using LuanVanTotNghiep.Models.Entities;
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
            //.Include(sp => sp.Branch) // Kéo theo dữ liệu Chi nhánh
            .FirstOrDefaultAsync(sp => sp.Id == id);
    }

    // Hàm đặc thù: Lấy các đợt đăng ký ĐANG MỞ CỔNG
    public async Task<IEnumerable<CaSchedulePeriod>> GetOpenPeriodsAsync()
    {
        return await _dbSet
           // .Include(sp => sp.Branch)
            .Where(sp => sp.Status == "OPEN")
            .ToListAsync();
    }
}
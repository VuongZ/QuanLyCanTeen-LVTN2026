using LuanVanTotNghiep.Models.Entities;
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
            .Include(c => c.Shift) // Chỉ kéo Shift, không kéo Branch nữa
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    // Hàm này tìm theo Shift thay vì Branch & Shift
    public async Task<CaBranchShiftConfig?> GetConfigByShiftAsync(int shiftId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(c => c.ShiftId == shiftId);
    }
    
    public async Task<IEnumerable<CaBranchShiftConfig>> GetAllConfigsAsync()
    {
        return await _dbSet
            .Include(c => c.Shift)  // Chỉ kéo Shift
            .ToListAsync();
    }
}
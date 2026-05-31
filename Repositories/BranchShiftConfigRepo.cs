using LuanVanTotNghiep.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Repositories;

public class BranchShiftConfigRepo : Repository<CaBranchShiftConfig>
{
    public override async Task<CaBranchShiftConfig?> GetbyId(int id)
    {
        return await _dbSet
            .Include(c => c.Branch) 
            .Include(c => c.Shift) 
            .FirstOrDefaultAsync(c => c.Id == id);
    }
    public BranchShiftConfigRepo(AppDbContext context) : base(context)
    {
    }

    // Hàm này lát nữa Service Đăng ký ca sẽ gọi liên tục để kiểm tra xem ca đã đầy chưa
    public async Task<CaBranchShiftConfig?> GetConfigByBranchAndShiftAsync(int branchId, int shiftId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(c => c.BranchId == branchId && c.ShiftId == shiftId);
    }
    
    // Lấy tất cả cấu hình (kèm tên nhánh và tên ca để hiển thị cho đẹp)
    public async Task<IEnumerable<CaBranchShiftConfig>> GetAllConfigsAsync()
    {
        return await _dbSet
            .Include(c => c.Branch) // Nhớ kiểm tra file Model xem tên property là Branch hay DmBranch nhé
            .Include(c => c.Shift)  // Kéo theo bảng CaShift
            .ToListAsync();
    }
}
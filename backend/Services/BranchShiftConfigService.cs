using LuanVanTotNghiep.Models.Entities;
using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Repositories;

namespace LuanVanTotNghiep.Services;

public class BranchShiftConfigService
{
    private readonly BranchShiftConfigRepo _repo;

    public BranchShiftConfigService(BranchShiftConfigRepo repo)
    {
        _repo = repo;
    }

    public async Task<IEnumerable<BranchShiftConfigDto>> GetAllAsync()
    {
        var configs = await _repo.GetAllConfigsAsync();
        return configs.Select(c => new BranchShiftConfigDto
        {
            Id = c.Id,
            BranchId = c.BranchId,
            ShiftId = c.ShiftId,
            MaxStaff = c.MaxStaff,
            BranchName = c.Branch?.Name, 
            ShiftName = c.Shift?.ShiftName
        }).ToList();
    }

    // Hàm này sẽ được React gọi khi admin bấm nút "Lưu cấu hình" trên ma trận
    public async Task BulkSaveConfigsAsync(List<CaBranchShiftConfig> incomingConfigs)
    {
        foreach (var config in incomingConfigs)
        {
            // Vì dữ liệu đã được tự động khởi tạo bằng 0 lúc tạo Ca, nên giờ ta chỉ cần CẬP NHẬT
            await _repo.Update(config);
        }
    }

    public async Task AddAsync(CaBranchShiftConfig config)
    {
        await _repo.Add(config);
    }

    public async Task UpdateAsync(CaBranchShiftConfig config)
    {
        await _repo.Update(config);
    }

    public async Task DeleteAsync(int id)
    {
        await _repo.Delete(id);
    }

    
}
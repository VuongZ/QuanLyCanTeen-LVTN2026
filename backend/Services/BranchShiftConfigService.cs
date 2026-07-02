using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.backend.Models.Entities;
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
            ShiftId = c.ShiftId,
            DayOfWeek = c.DayOfWeek,
            MaxStaff = c.MaxStaff,
            ShiftName = c.Shift?.ShiftName
        }).ToList();
    }

    public async Task AddAsync(SaveShiftConfigDto dto)
    {
        var newConfig = new CaBranchShiftConfig
        {
            ShiftId = dto.ShiftId,
            DayOfWeek = dto.DayOfWeek,
            MaxStaff = dto.MaxStaff
        };
        await _repo.Add(newConfig);
    }

    public async Task UpdateAsync(int id, SaveShiftConfigDto dto)
    {
        var existing = await _repo.GetbyId(id);
        if (existing == null) throw new KeyNotFoundException("Không tìm thấy cấu hình");

        existing.ShiftId = dto.ShiftId;
        existing.DayOfWeek = dto.DayOfWeek;
        existing.MaxStaff = dto.MaxStaff;

        await _repo.Update(existing);
    }

    public async Task DeleteAsync(int id)
    {
        // Kiểm tra xem cấu hình có tồn tại không trước khi xóa
        var existing = await _repo.GetbyId(id);
        if (existing == null) 
            throw new KeyNotFoundException("Không tìm thấy cấu hình để xóa.");

        await _repo.Delete(id);
    }
}
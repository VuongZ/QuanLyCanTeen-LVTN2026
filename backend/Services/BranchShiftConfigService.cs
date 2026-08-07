using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Services;

public class BranchShiftConfigService
{
    private readonly BranchShiftConfigRepo _repo;
    private readonly AppDbContext _context;

    public BranchShiftConfigService(
        BranchShiftConfigRepo repo,
        AppDbContext context)
    {
        _repo = repo;
        _context = context;
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
        await EnsureShiftCanBeConfiguredAsync(dto.ShiftId);

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
        if (existing == null)
        {
            throw new KeyNotFoundException("Không tìm thấy cấu hình.");
        }

        await EnsureShiftCanBeConfiguredAsync(dto.ShiftId);

        existing.ShiftId = dto.ShiftId;
        existing.DayOfWeek = dto.DayOfWeek;
        existing.MaxStaff = dto.MaxStaff;

        await _repo.Update(existing);
    }

    public async Task DeleteAsync(int id)
    {
        var existing = await _repo.GetbyId(id);
        if (existing == null)
        {
            throw new KeyNotFoundException("Không tìm thấy cấu hình để xóa.");
        }

        await EnsureShiftCanBeConfiguredAsync(existing.ShiftId);
        await _repo.Delete(id);
    }

    private async Task EnsureShiftCanBeConfiguredAsync(int shiftId)
    {
        var shift = await _context.CaShifts
            .AsNoTracking()
            .Include(item => item.Branch)
            .FirstOrDefaultAsync(item => item.Id == shiftId);

        if (shift == null)
        {
            throw new KeyNotFoundException("Không tìm thấy ca làm.");
        }

        if (!shift.IsActive)
        {
            throw new InvalidOperationException(
                "Ca làm đã ngừng hoạt động nên không thể thay đổi cấu hình.");
        }

        if (shift.Branch == null || !shift.Branch.IsActive)
        {
            throw new InvalidOperationException(
                "Cơ sở đã ngừng hoạt động nên không thể thay đổi cấu hình ca.");
        }
    }
}

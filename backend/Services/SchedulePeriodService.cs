using LuanVanTotNghiep.Models.Entities;
using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Repositories;

namespace LuanVanTotNghiep.Services;

public class SchedulePeriodService
{
    private readonly SchedulePeriodRepo _repo;

    public SchedulePeriodService(SchedulePeriodRepo repo)
    {
        _repo = repo;
    }

    public async Task<IEnumerable<SchedulePeriodDto>> GetAllAsync()
    {
        var periods = await _repo.GetAll();
        return periods.Select(p => new SchedulePeriodDto
        {
            Id = p.Id,
            BranchId = p.BranchId,
            StartDate = p.StartDate,
            EndDate = p.EndDate,
            Status = p.Status
        }).ToList();
    }

    // Lấy các đợt đang mở để hiển thị cho Nhân viên đăng ký
    public async Task<IEnumerable<SchedulePeriodDto>> GetOpenPeriodsAsync()
    {
        var periods = await _repo.GetOpenPeriodsAsync();
        return periods.Select(p => new SchedulePeriodDto
        {
            Id = p.Id,
            BranchId = p.BranchId,
            BranchName = p?.Branch?.Name,
            StartDate = p.StartDate,
            EndDate = p.EndDate,
            Status = p?.Status
        }).ToList();
    }

    public async Task AddAsync(CaSchedulePeriod period)
    {
        // Mặc định tạo đợt mới thì trạng thái là OPEN
        period.Status = "OPEN"; 
        await _repo.Add(period);
    }

    public async Task UpdateAsync(CaSchedulePeriod period)
    {
        await _repo.Update(period);
    }

    public async Task DeleteAsync(int id)
    {
        await _repo.Delete(id);
    }
}
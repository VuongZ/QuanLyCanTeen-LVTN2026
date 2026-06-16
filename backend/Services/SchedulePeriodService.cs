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
           // BranchName = p?.Branch?.Name,
            StartDate = p.StartDate,
            EndDate = p.EndDate,
            Status = p?.Status
        }).ToList();
    }

  public async Task AddAsync(CreatePeriodDto dto)
    {
        if (dto.EndDate < dto.StartDate) 
            throw new ArgumentException("Ngày kết thúc không hợp lệ.");

        // Chuyển DTO thành Entity
        var period = new CaSchedulePeriod
        {
            BranchId = dto.BranchId,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Status = "OPEN", // Ép cứng mặc định là OPEN an toàn
            CreatedAt = DateTime.Now
        };

        await _repo.Add(period);
    }

  public async Task UpdateAsync(int id, UpdatePeriodDto dto)
    {
        if (dto.EndDate < dto.StartDate) 
            throw new ArgumentException("Ngày kết thúc không hợp lệ.");

        // 1. Phải tìm đợt đăng ký cũ dưới DB lên trước
        var existingPeriod = await _repo.GetbyId(id);
        if (existingPeriod == null) 
            throw new KeyNotFoundException("Không tìm thấy đợt đăng ký.");

        // 2. Chỉ cập nhật những trường được phép
        existingPeriod.StartDate = dto.StartDate;
        existingPeriod.EndDate = dto.EndDate;
        existingPeriod.Status = dto.Status;

        await _repo.Update(existingPeriod);
    }

    public async Task DeleteAsync(int id)
    {
        await _repo.Delete(id);
    }
}
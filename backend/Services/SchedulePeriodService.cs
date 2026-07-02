using LuanVanTotNghiep.backend.Models.Entities;
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
    var today = DateOnly.FromDateTime(DateTime.Today);
    if (dto.StartDate < today)
        throw new ArgumentException("Không thể tạo đợt đăng ký lịch làm việc ở quá khứ.");

    // 1. Chỉ cần kiểm tra đúng ngày Thứ Hai
    if (dto.StartDate.DayOfWeek != DayOfWeek.Monday)
        throw new ArgumentException("Đợt đăng ký ca bắt buộc phải bắt đầu vào ngày Thứ Hai.");

    // 2. XÓA BỎ các rào kiểm tra EndDate rườm rà.
    // Tự động tính ngày Chủ Nhật bằng mã C# luôn
    var autoEndDate = dto.StartDate.AddDays(6);

    var period = new CaSchedulePeriod
    {
        BranchId = dto.BranchId,
        StartDate = dto.StartDate,
        EndDate = autoEndDate, // Dùng ngày tự tính, phớt lờ data Frontend gửi lên
        Status = "OPEN", 
        CreatedAt = DateTime.Now
    };

    await _repo.Add(period);
}

 public async Task UpdateAsync(int id, UpdatePeriodDto dto)
{
    if (dto.StartDate.DayOfWeek != DayOfWeek.Monday)
        throw new ArgumentException("Ngày bắt đầu phải là Thứ Hai.");

    var existingPeriod = await _repo.GetbyId(id);
    if (existingPeriod == null) 
        throw new KeyNotFoundException("Không tìm thấy đợt đăng ký.");

    // Tự động tính lại ngày kết thúc
    existingPeriod.StartDate = dto.StartDate;
    existingPeriod.EndDate = dto.StartDate.AddDays(6); 
    existingPeriod.Status = dto.Status;

    await _repo.Update(existingPeriod);
}

public async Task UpdateStatusOnlyAsync(int id, string newStatus)
{
    var period = await _repo.GetbyId(id);
    if (period == null)
        throw new KeyNotFoundException("Không tìm thấy đợt đăng ký.");
    
    period.Status = newStatus;
    await _repo.Update(period);
}

    public async Task DeleteAsync(int id)
    {
        await _repo.Delete(id);
    }
}
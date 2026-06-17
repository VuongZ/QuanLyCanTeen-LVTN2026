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
        throw new ArgumentException("Ngày kết thúc không thể nhỏ hơn ngày bắt đầu.");

    // 1. VŨ KHÍ MỚI: Chặn không cho tạo lịch ở quá khứ
    var today = DateOnly.FromDateTime(DateTime.Today);
    if (dto.StartDate < today)
        throw new ArgumentException("Không thể tạo đợt đăng ký lịch làm việc ở quá khứ.");

    // 2. RÀO THỨ HAI: Kiểm tra ngày bắt đầu phải là Thứ Hai
    if (dto.StartDate.DayOfWeek != DayOfWeek.Monday)
        throw new ArgumentException("Đợt đăng ký ca bắt buộc phải bắt đầu vào ngày Thứ Hai.");

    // 3. RÀO CHỦ NHẬT: Kiểm tra ngày kết thúc phải là Chủ Nhật
    if (dto.EndDate.DayOfWeek != DayOfWeek.Sunday)
        throw new ArgumentException("Đợt đăng ký ca bắt buộc phải kết thúc vào ngày Chủ Nhật.");

    // 4. KIỂM TRA TRÒN TUẦN: Đảm bảo đợt đăng ký kéo dài đúng 7 ngày
    if (dto.EndDate.DayNumber - dto.StartDate.DayNumber != 6)
        throw new ArgumentException("Một đợt đăng ký ca phải kéo dài trọn vẹn 7 ngày (từ Thứ Hai đến Chủ Nhật).");

    // Chuyển DTO thành Entity và lưu vào DB
    var period = new CaSchedulePeriod
    {
        BranchId = dto.BranchId,
        StartDate = dto.StartDate,
        EndDate = dto.EndDate,
        Status = "OPEN", 
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
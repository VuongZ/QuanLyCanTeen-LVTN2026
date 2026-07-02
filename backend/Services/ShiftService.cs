using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Repositories;
using LuanVanTotNghiep.backend.Models.Entities;

namespace LuanVanTotNghiep.Services;

public class ShiftService
{
    private readonly ShiftRepo _repo;
    private readonly BranchShiftConfigRepo _configRepo; 
    private readonly AppDbContext _context; 

    // GỘP LẠI THÀNH 1 CONSTRUCTOR DUY NHẤT:
    public ShiftService(ShiftRepo repo, BranchShiftConfigRepo configRepo, AppDbContext context)
    {
        _repo = repo;
        _configRepo = configRepo;
        _context = context;
    }

    public async Task<IEnumerable<ShiftDto>> GetAllShiftsAsync()
    {
        var shifts = await _repo.GetAll();
        return shifts.Select(s=>new ShiftDto
        {
            Id = s.Id,
            ShiftName = s.ShiftName,
            StartTime = s.StartTime,
            EndTime = s.EndTime,
            MaxStaff = s.MaxStaff,
            IsOt = s.IsOt,
            BranchId = s.BranchId
        }).ToList();  
    }

    public async Task<ShiftDto?> GetShiftByIdAsync(int id)
    {
        var shift = await _repo.GetbyId(id);
        if (shift == null) return null;

        return new ShiftDto
        {
            Id = shift.Id,
            ShiftName = shift.ShiftName,
            StartTime = shift.StartTime,
            EndTime = shift.EndTime,
            MaxStaff = shift.MaxStaff,
            IsOt = shift.IsOt,
            BranchName = shift.Branch?.Name 
        };
    }

    // HÀM TẠO CA LÀM MỚI (TÍCH HỢP ĐẺ AUTO 7 NGÀY CONFIG)
    public async Task<CaShift> CreateShiftWithAutoConfigAsync(CreateShiftDto dto)
    {
        var newShift = new CaShift
        {
            BranchId = dto.BranchId,
            ShiftName = dto.ShiftName,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            MaxStaff = dto.MaxStaff,
            IsOt = dto.IsOt 
        };

        // Lưu Ca vào DB trước để lấy ID
        await _context.CaShifts.AddAsync(newShift);
        await _context.SaveChangesAsync();

        var daysOfWeek = new List<string> { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
        var configs = new List<CaBranchShiftConfig>();

        foreach (var day in daysOfWeek)
        {
            configs.Add(new CaBranchShiftConfig
            {
                ShiftId = newShift.Id, 
                DayOfWeek = day,
                MaxStaff = newShift.MaxStaff 
            });
        }

        // Lưu 7 cái Config này xuống DB
        await _context.CaBranchShiftConfigs.AddRangeAsync(configs);
        await _context.SaveChangesAsync();

        return newShift;
    }

    public async Task UpdateShiftAsync(CaShift shift)
    {
        await _repo.Update(shift);
    }

    public async Task DeleteShiftAsync(int id)
    {
        await _repo.Delete(id);
    }
}
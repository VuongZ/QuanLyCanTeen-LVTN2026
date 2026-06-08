using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Repositories;
using LuanVanTotNghiep.Models.Entities;

namespace LuanVanTotNghiep.Services;
public class ShiftService
{
    private readonly ShiftRepo _repo;

    // 1. Thêm dòng khai báo thủ kho Cấu hình vào đây:
    private readonly BranchShiftConfigRepo _configRepo; 

    // 2. Chèn configRepo vào Constructor:
    public  ShiftService(ShiftRepo repo, BranchShiftConfigRepo configRepo)
    {
        _repo = repo;
        _configRepo = configRepo;
    }
    public async Task<IEnumerable<ShiftDto>> GetAllShiftsAsync()
    {
        var shifts = await _repo.GetAll();
        return shifts.Select(s=>new ShiftDto
        {
            Id=s.Id,
            ShiftName=s.ShiftName,
            StartTime=s.StartTime,
            EndTime=s.EndTime,
            MaxStaff=s.MaxStaff,
            IsOt=s.IsOt
        }).ToList();  
    }

    // ĐỌC: Lấy 1 ca làm theo ID
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
            BranchName = shift.Branch?.Name // Nhờ hàm Include bên Repo nên mới lấy được Tên nhánh
        };
    }

  // THÊM & KHỞI TẠO TỰ ĐỘNG
    public async Task AddShiftAsync(CaShift shift)
    {
        // 1. Lưu ca làm mới vào DB trước để SQL tạo ra cái ID (shift.Id)
        await _repo.Add(shift); 

        // 2. Ngay lập tức khởi tạo 7 dòng cấu hình bằng 0 cho ca vừa tạo
        var daysOfWeek = new List<string> 
        { 
            "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" 
        };

        foreach (var day in daysOfWeek)
        {
            var config = new CaBranchShiftConfig
            {
                BranchId = shift.BranchId ?? 0, // Lấy ID chi nhánh của ca đó
                ShiftId = shift.Id,        // Lấy ID ca làm vừa mới được tạo ra
                DayOfWeek = day,
                MaxStaff = 0               // Mặc định bằng 0 theo đúng ý bạn
            };
            
            await _configRepo.Add(config);
        }
    }

    // SỬA và XÓA (Giống hệt BranchService, vì Repo đã lo phần Database)
    public async Task UpdateShiftAsync(CaShift shift)
    {
        await _repo.Update(shift);
    }

    public async Task DeleteShiftAsync(int id)
    {
        await _repo.Delete(id);
    }
}
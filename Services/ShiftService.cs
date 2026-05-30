using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Repositories;
using LuanVanTotNghiep.Models.Entities;

namespace LuanVanTotNghiep.Services;
public class ShiftService
{
    private readonly ShiftRepo? _repo;
    public ShiftService(ShiftRepo repo)
    {
        _repo=repo;
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

    // THÊM: Tạo ca làm mới từ dữ liệu thô
    public async Task AddShiftAsync(CaShift shift)
    {
        await _repo.Add(shift);
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
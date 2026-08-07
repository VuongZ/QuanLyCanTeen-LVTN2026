using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Repositories;
using LuanVanTotNghiep.backend.Models.Entities;

namespace LuanVanTotNghiep.Services;

public class ShiftService
{
    private readonly ShiftRepo _repo;
    private readonly BranchShiftConfigRepo _configRepo;
    private readonly AppDbContext _context;

    public ShiftService(
        ShiftRepo repo,
        BranchShiftConfigRepo configRepo,
        AppDbContext context)
    {
        _repo = repo;
        _configRepo = configRepo;
        _context = context;
    }

    public async Task<IEnumerable<ShiftDto>> GetAllShiftsAsync(
        bool includeInactive = false)
    {
        var shifts = await _repo.GetAllShiftsAsync(includeInactive);
        return shifts.Select(ToDto).ToList();
    }

    public async Task<ShiftDto?> GetShiftByIdAsync(int id)
    {
        var shift = await _repo.GetbyId(id);
        return shift == null ? null : ToDto(shift);
    }

    public async Task<CaShift> CreateShiftWithAutoConfigAsync(
        CreateShiftDto dto)
    {
        if (!await _repo.IsBranchActiveAsync(dto.BranchId))
        {
            throw new InvalidOperationException(
                "Cơ sở đã ngừng hoạt động nên không thể tạo ca làm mới.");
        }

        var newShift = new CaShift
        {
            BranchId = dto.BranchId,
            ShiftName = dto.ShiftName.Trim(),
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            MaxStaff = dto.MaxStaff,
            IsOt = dto.IsOt,
            IsActive = true,
            InactiveAt = null,
            InactiveBy = null,
            InactiveReason = null
        };

        await _context.CaShifts.AddAsync(newShift);
        await _context.SaveChangesAsync();

        var daysOfWeek = new List<string>
        {
            "Monday",
            "Tuesday",
            "Wednesday",
            "Thursday",
            "Friday",
            "Saturday",
            "Sunday"
        };

        var configs = daysOfWeek
            .Select(day => new CaBranchShiftConfig
            {
                ShiftId = newShift.Id,
                DayOfWeek = day,
                MaxStaff = newShift.MaxStaff
            })
            .ToList();

        await _context.CaBranchShiftConfigs.AddRangeAsync(configs);
        await _context.SaveChangesAsync();

        return newShift;
    }

    public async Task<ShiftDto> UpdateShiftAsync(
        int id,
        CaShift input)
    {
        var shift = await _repo.GetbyId(id)
            ?? throw new KeyNotFoundException("Không tìm thấy ca làm.");

        if (!shift.IsActive)
        {
            throw new InvalidOperationException(
                "Ca làm đã ngừng hoạt động. Hãy khôi phục trước khi chỉnh sửa.");
        }

        if (!shift.BranchId.HasValue ||
            !await _repo.IsBranchActiveAsync(shift.BranchId.Value))
        {
            throw new InvalidOperationException(
                "Cơ sở của ca làm đã ngừng hoạt động.");
        }

        shift.ShiftName = input.ShiftName.Trim();
        shift.StartTime = input.StartTime;
        shift.EndTime = input.EndTime;
        shift.MaxStaff = input.MaxStaff;
        shift.IsOt = input.IsOt;

        await _repo.Update(shift);
        return ToDto(shift);
    }

    public async Task<ShiftDto> DeactivateShiftAsync(
        int id,
        int adminUserId,
        string? reason)
    {
        var shift = await _repo.GetbyId(id)
            ?? throw new KeyNotFoundException("Không tìm thấy ca làm.");

        if (!shift.IsActive)
        {
            return ToDto(shift);
        }

        shift.IsActive = false;
        shift.InactiveAt = DateTime.Now;
        shift.InactiveBy = adminUserId > 0 ? adminUserId : null;
        shift.InactiveReason = NormalizeReason(reason);

        await _repo.Update(shift);
        return ToDto(shift);
    }

    public async Task<ShiftDto> RestoreShiftAsync(int id)
    {
        var shift = await _repo.GetbyId(id)
            ?? throw new KeyNotFoundException("Không tìm thấy ca làm.");

        if (!shift.BranchId.HasValue ||
            !await _repo.IsBranchActiveAsync(shift.BranchId.Value))
        {
            throw new InvalidOperationException(
                "Cơ sở đang ngừng hoạt động. Hãy khôi phục cơ sở trước khi khôi phục ca làm.");
        }

        shift.IsActive = true;
        shift.InactiveAt = null;
        shift.InactiveBy = null;
        shift.InactiveReason = null;

        await _repo.Update(shift);
        return ToDto(shift);
    }

    private static string? NormalizeReason(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return null;
        }

        var normalized = reason.Trim();
        return normalized.Length <= 255
            ? normalized
            : normalized[..255];
    }

    private static ShiftDto ToDto(CaShift shift)
    {
        return new ShiftDto
        {
            Id = shift.Id,
            ShiftName = shift.ShiftName,
            StartTime = shift.StartTime,
            EndTime = shift.EndTime,
            MaxStaff = shift.MaxStaff,
            IsOt = shift.IsOt,
            BranchId = shift.BranchId,
            BranchName = shift.Branch?.Name,
            IsActive = shift.IsActive,
            InactiveAt = shift.InactiveAt,
            InactiveBy = shift.InactiveBy,
            InactiveReason = shift.InactiveReason
        };
    }
}

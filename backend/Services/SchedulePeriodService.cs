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

    private static string NormalizePeriodStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            throw new ArgumentException("Trạng thái đợt đăng ký không được để trống.");

        var normalizedStatus = status.Trim().ToUpperInvariant();

        return normalizedStatus switch
        {
            "MỞ" => "OPEN",
            "MO" => "OPEN",
            "OPEN" => "OPEN",

            "ĐÓNG" => "CLOSED",
            "DONG" => "CLOSED",
            "CLOSED" => "CLOSED",
            "REVIEWING" => "CLOSED",
            "DRAFT" => "CLOSED",

            "PUBLISHED" => "PUBLISHED",
            "ĐÃ CHỐT" => "PUBLISHED",
            "DA CHOT" => "PUBLISHED",

            _ => throw new ArgumentException(
                "Trạng thái đợt đăng ký không hợp lệ.")
        };
    }

    private static DateOnly GetVietnamToday()
    {
        var timeZoneId = OperatingSystem.IsWindows()
            ? "SE Asia Standard Time"
            : "Asia/Ho_Chi_Minh";

        var vietnamTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

        var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            vietnamTimeZone);

        return DateOnly.FromDateTime(vietnamNow);
    }

    public async Task<IEnumerable<SchedulePeriodDto>> GetAllAsync()
    {
        var periods = await _repo.GetAll();

        return periods
            .Select(period => new SchedulePeriodDto
            {
                Id = period.Id,
                BranchId = period.BranchId,
                StartDate = period.StartDate,
                EndDate = period.EndDate,
                Status = period.Status
            })
            .ToList();
    }

    // Lấy các đợt đang mở và chưa đến ngày bắt đầu để Nhân viên đăng ký.
    public async Task<IEnumerable<SchedulePeriodDto>> GetOpenPeriodsAsync()
    {
        var today = GetVietnamToday();
        var periods = await _repo.GetOpenPeriodsAsync();

        return periods
            .Where(period =>
                string.Equals(
                    period.Status,
                    "OPEN",
                    StringComparison.OrdinalIgnoreCase) &&
                today < period.StartDate)
            .Select(period => new SchedulePeriodDto
            {
                Id = period.Id,
                BranchId = period.BranchId,
                StartDate = period.StartDate,
                EndDate = period.EndDate,
                Status = period.Status
            })
            .ToList();
    }

    public async Task AddAsync(CreatePeriodDto dto)
    {
        var today = GetVietnamToday();

        if (dto.StartDate <= today)
        {
            throw new ArgumentException(
                "Ngày bắt đầu của đợt đăng ký phải lớn hơn ngày hiện tại.");
        }

        if (dto.StartDate.DayOfWeek != DayOfWeek.Monday)
        {
            throw new ArgumentException(
                "Đợt đăng ký ca bắt buộc phải bắt đầu vào ngày Thứ Hai.");
        }

        var period = new CaSchedulePeriod
        {
            BranchId = dto.BranchId,
            StartDate = dto.StartDate,
            EndDate = dto.StartDate.AddDays(6),
            Status = "OPEN",
            CreatedAt = DateTime.Now
        };

        await _repo.Add(period);
    }

    public async Task UpdateAsync(int id, UpdatePeriodDto dto)
    {
        var existingPeriod = await _repo.GetbyId(id);

        if (existingPeriod == null)
            throw new KeyNotFoundException("Không tìm thấy đợt đăng ký.");

        if (string.Equals(
                existingPeriod.Status,
                "PUBLISHED",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Không thể chỉnh sửa đợt đăng ký đã được công bố.");
        }

        var today = GetVietnamToday();

        if (dto.StartDate <= today)
        {
            throw new ArgumentException(
                "Ngày bắt đầu của đợt đăng ký phải lớn hơn ngày hiện tại.");
        }

        if (dto.StartDate.DayOfWeek != DayOfWeek.Monday)
        {
            throw new ArgumentException(
                "Ngày bắt đầu phải là Thứ Hai.");
        }

        var normalizedStatus = NormalizePeriodStatus(dto.Status);

        if (normalizedStatus == "PUBLISHED")
        {
            throw new InvalidOperationException(
                "Phải sử dụng chức năng công bố lịch để chuyển đợt sang trạng thái đã công bố.");
        }

        existingPeriod.StartDate = dto.StartDate;
        existingPeriod.EndDate = dto.StartDate.AddDays(6);
        existingPeriod.Status = normalizedStatus;

        await _repo.Update(existingPeriod);
    }

    public async Task UpdateStatusOnlyAsync(int id, string newStatus)
    {
        var period = await _repo.GetbyId(id);

        if (period == null)
            throw new KeyNotFoundException("Không tìm thấy đợt đăng ký.");

        var normalizedStatus = NormalizePeriodStatus(newStatus);
        var today = GetVietnamToday();

        if (string.Equals(
                period.Status,
                "PUBLISHED",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Đợt đăng ký đã được công bố nên không thể thay đổi trạng thái.");
        }

        if (normalizedStatus == "PUBLISHED")
        {
            throw new InvalidOperationException(
                "Phải sử dụng chức năng công bố lịch để chuyển đợt sang trạng thái đã công bố.");
        }

        if (normalizedStatus == "OPEN" &&
            today >= period.StartDate)
        {
            throw new InvalidOperationException(
                "Không thể mở lại đợt đăng ký khi đã đến ngày bắt đầu lịch làm.");
        }

        period.Status = normalizedStatus;
        await _repo.Update(period);
    }

    public async Task DeleteAsync(int id)
    {
        var period = await _repo.GetbyId(id);

        if (period == null)
            throw new KeyNotFoundException("Không tìm thấy đợt đăng ký.");

        if (string.Equals(
                period.Status,
                "PUBLISHED",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Không thể xóa đợt đăng ký đã được công bố.");
        }

        await _repo.Delete(id);
    }

    // Worker gọi phương thức này để tự động khóa
    // các đợt OPEN đã đến ngày bắt đầu.
    public async Task<int> CloseExpiredOpenPeriodsAsync(
        CancellationToken cancellationToken = default)
    {
        var today = GetVietnamToday();

        return await _repo.CloseExpiredOpenPeriodsAsync(
            today,
            cancellationToken);
    }
}
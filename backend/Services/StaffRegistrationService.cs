using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Repositories;

namespace LuanVanTotNghiep.Services;

public class StaffRegistrationService
{
    private readonly StaffRegistrationRepo _repo;

    public StaffRegistrationService(StaffRegistrationRepo repo)
    {
        _repo = repo;
    }

    private static TimeZoneInfo GetVietnamTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(
                "SE Asia Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById(
                "Asia/Ho_Chi_Minh");
        }
    }

    private static DateOnly GetVietnamToday()
    {
        var vietnamTimeZone = GetVietnamTimeZone();

        var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            vietnamTimeZone);

        return DateOnly.FromDateTime(vietnamNow);
    }

    // Nhân viên đăng ký ca theo nguyên tắc ai đăng ký trước được nhận trước.
    public async Task<CaStaffRegistration> RegisterAsync(
        RegisterShiftDto dto)
    {
        await using var transaction =
            await _repo.BeginSerializableTransactionAsync();

        // 1. Kiểm tra đợt đăng ký.
        var period = await _repo.GetPeriodByIdAsync(dto.PeriodId);

        if (period == null)
        {
            throw new KeyNotFoundException(
                "Đợt đăng ký không tồn tại.");
        }

        if (!string.Equals(
                period.Status,
                "OPEN",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Đợt đăng ký đã khóa hoặc đã công bố.");
        }

        var today = GetVietnamToday();

        if (today >= period.StartDate)
        {
            throw new InvalidOperationException(
                "Đợt đăng ký đã hết thời gian tiếp nhận đăng ký ca.");
        }

        if (dto.WorkDate < period.StartDate ||
            dto.WorkDate > period.EndDate)
        {
            throw new ArgumentException(
                "Ngày đăng ký không nằm trong thời gian của đợt này.");
        }

        // 2. Kiểm tra Nhân viên.
        var user = await _repo.GetUserByIdAsync(dto.UserId);

        if (user == null)
        {
            throw new KeyNotFoundException(
                "Không tìm thấy nhân viên.");
        }

        if (user.BranchId != period.BranchId)
        {
            throw new InvalidOperationException(
                "Bạn chỉ được đăng ký ca làm tại chi nhánh của mình.");
        }

        // 3. Kiểm tra ca làm.
        var shift = await _repo.GetShiftByIdAsync(dto.ShiftId);

        if (shift == null)
        {
            throw new KeyNotFoundException(
                "Không tìm thấy ca làm.");
        }

        if (shift.BranchId != period.BranchId)
        {
            throw new InvalidOperationException(
                "Ca làm không thuộc chi nhánh của đợt đăng ký.");
        }

        // 4. Kiểm tra ca có hoạt động trong ngày đã chọn.
        var targetDay = dto.WorkDate.DayOfWeek.ToString();

        var config = await _repo.GetShiftConfigAsync(
            dto.ShiftId,
            targetDay);

        if (config == null || config.MaxStaff <= 0)
        {
            string[] vietnameseDays =
            {
                "Chủ nhật",
                "Thứ 2",
                "Thứ 3",
                "Thứ 4",
                "Thứ 5",
                "Thứ 6",
                "Thứ 7"
            };

            var vietnameseDayName =
                vietnameseDays[(int)dto.WorkDate.DayOfWeek];

            throw new InvalidOperationException(
                $"Ca làm này không mở vào {vietnameseDayName}.");
        }

        var cancelledStatuses = new[]
        {
            "CANCELLED",
            "REJECTED",
            "Từ Chối"
        };

        // 5. Kiểm tra đăng ký trùng.
        var isDuplicate =
            await _repo.HasActiveRegistrationAsync(
                dto.PeriodId,
                dto.UserId,
                dto.ShiftId,
                dto.WorkDate,
                cancelledStatuses);

        if (isDuplicate)
        {
            throw new InvalidOperationException(
                "Bạn đã đăng ký ca này vào ngày này rồi.");
        }

        // 6. Kiểm tra số lượng Nhân viên.
        // Manager chiếm một vị trí trong tổng số người của ca.
        var maxStaff = config.MaxStaff.GetValueOrDefault();
        var staffSlot = Math.Max(maxStaff - 1, 0);

        if (staffSlot <= 0)
        {
            throw new InvalidOperationException(
                "Ca làm này chỉ có vị trí cho Quản lý, " +
                "Nhân viên không thể đăng ký.");
        }

        var registeredCount =
            await _repo.CountActiveRegistrationsAsync(
                dto.PeriodId,
                dto.ShiftId,
                dto.WorkDate,
                cancelledStatuses);

        if (registeredCount >= staffSlot)
        {
            throw new InvalidOperationException(
                "Ca đã đủ số lượng Nhân viên, " +
                "bạn không thể đăng ký vào ca này.");
        }

        // 7. Lưu đăng ký.
        var registration = new CaStaffRegistration
        {
            UserId = dto.UserId,
            PeriodId = dto.PeriodId,
            ShiftId = dto.ShiftId,
            WorkDate = dto.WorkDate,
            Status = "REGISTERED"
        };

        await _repo.Add(registration);
        await transaction.CommitAsync();

        return registration;
    }

    // Nhân viên xem các ca đã đăng ký trong một đợt.
    public async Task<IEnumerable<CaStaffRegistration>>
        GetMyScheduleAsync(
            int userId,
            int periodId)
    {
        return await _repo.GetMyRegistrationsAsync(
            userId,
            periodId);
    }

    // Manager xem danh sách đăng ký của một đợt.
    public async Task<IEnumerable<CaStaffRegistration>>
        GetRegistrationsByPeriodAsync(int periodId)
    {
        return await _repo.GetRegistrationsByPeriodAsync(
            periodId);
    }

    // Cập nhật trạng thái đăng ký khi lịch chưa công bố.
   public async Task UpdateStatusAsync(
    int registrationId,
    string newStatus)
{
    var registration =
        await _repo.GetbyId(registrationId);

    if (registration == null)
    {
        throw new KeyNotFoundException(
            "Không tìm thấy phiếu đăng ký này.");
    }

    if (registration.PeriodId is not int periodId)
    {
        throw new InvalidOperationException(
            "Phiếu đăng ký không thuộc đợt đăng ký hợp lệ.");
    }

    var period =
        await _repo.GetPeriodByIdAsync(periodId);

    if (period == null)
    {
        throw new KeyNotFoundException(
            "Không tìm thấy đợt đăng ký.");
    }

    if (string.Equals(
            period.Status,
            "PUBLISHED",
            StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException(
            "Lịch đã được công bố nên không thể thay đổi đăng ký.");
    }

    if (string.IsNullOrWhiteSpace(newStatus))
    {
        throw new ArgumentException(
            "Trạng thái đăng ký không được để trống.");
    }

    var normalizedStatus =
        newStatus.Trim().ToUpperInvariant();

    if (normalizedStatus != "REGISTERED" &&
        normalizedStatus != "CANCELLED")
    {
        throw new ArgumentException(
            "Trạng thái đăng ký không hợp lệ.");
    }

    registration.Status = normalizedStatus;

    await _repo.Update(registration);
}

    // Nhân viên hủy ca khi đợt còn mở và chưa đến hạn.
    public async Task CancelRegistrationAsync(
    int id,
    int userId)
{
    var registration = await _repo.GetbyId(id);

    if (registration == null)
    {
        throw new KeyNotFoundException(
            "Không tìm thấy phiếu đăng ký này.");
    }

    if (registration.UserId != userId)
    {
        throw new InvalidOperationException(
            "Bạn không có quyền hủy ca của người khác.");
    }

    if (registration.PeriodId is not int periodId)
    {
        throw new InvalidOperationException(
            "Phiếu đăng ký không thuộc đợt đăng ký hợp lệ.");
    }

    var period =
        await _repo.GetPeriodByIdAsync(periodId);

    if (period == null)
    {
        throw new KeyNotFoundException(
            "Không tìm thấy đợt đăng ký.");
    }

    if (!string.Equals(
            period.Status,
            "OPEN",
            StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException(
            "Đợt đăng ký đã khóa hoặc đã công bố, " +
            "không thể hủy ca.");
    }

    var today = GetVietnamToday();

    if (today >= period.StartDate)
    {
        throw new InvalidOperationException(
            "Đợt đăng ký đã hết hạn, không thể hủy ca.");
    }

    if (!string.Equals(
            registration.Status,
            "REGISTERED",
            StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException(
            "Ca đăng ký này không thể hủy.");
    }

    registration.Status = "CANCELLED";
    await _repo.Update(registration);
}
}
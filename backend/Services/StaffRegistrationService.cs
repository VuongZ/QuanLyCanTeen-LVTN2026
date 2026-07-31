using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Repositories;

namespace LuanVanTotNghiep.Services;

public class StaffRegistrationService
{
    private const string RegisteredStatus = "REGISTERED";
    private const string WaitlistStatus = "WAITLIST";
    private const string CancelledStatus = "CANCELLED";

    private readonly StaffRegistrationRepo _repo;

    public StaffRegistrationService(
        StaffRegistrationRepo repo)
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

    private static DateTime GetVietnamNow()
    {
        return TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            GetVietnamTimeZone());
    }

    private static DateOnly GetVietnamToday()
    {
        return DateOnly.FromDateTime(
            GetVietnamNow());
    }

    /// <summary>
    /// Nhân viên đăng ký ca theo thứ tự đăng ký.
    ///
    /// Còn chỗ:
    ///     REGISTERED
    ///
    /// Hết chỗ:
    ///     WAITLIST
    /// </summary>
    public async Task<CaStaffRegistration> RegisterAsync(
        RegisterShiftDto dto)
    {
        await using var transaction =
            await _repo.BeginSerializableTransactionAsync();

        var period =
            await _repo.GetPeriodByIdAsync(dto.PeriodId);

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

        var user =
            await _repo.GetUserByIdAsync(dto.UserId);

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

        var shift =
            await _repo.GetShiftByIdAsync(dto.ShiftId);

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

        var targetDay =
            dto.WorkDate.DayOfWeek.ToString();

        var config =
            await _repo.GetShiftConfigAsync(
                dto.ShiftId,
                targetDay);

        if (config == null ||
            config.MaxStaff.GetValueOrDefault() <= 0)
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
                vietnameseDays[
                    (int)dto.WorkDate.DayOfWeek];

            throw new InvalidOperationException(
                $"Ca làm này không mở vào {vietnameseDayName}.");
        }

        var isDuplicate =
            await _repo
                .HasNonCancelledRegistrationAsync(
                    dto.PeriodId,
                    dto.UserId,
                    dto.ShiftId,
                    dto.WorkDate);

        if (isDuplicate)
        {
            throw new InvalidOperationException(
                "Bạn đã đăng ký ca này vào ngày này rồi.");
        }

        // Manager được hệ thống tự thêm khi công bố lịch,
        // nên chiếm một vị trí trong MaxStaff.
        var maxStaff =
            config.MaxStaff.GetValueOrDefault();

        var staffSlot =
            Math.Max(maxStaff - 1, 0);

        if (staffSlot <= 0)
        {
            throw new InvalidOperationException(
                "Ca làm này chỉ có vị trí cho Quản lý, " +
                "Nhân viên không thể đăng ký.");
        }

        // Chỉ đếm REGISTERED.
        // WAITLIST không chiếm vị trí chính thức.
        var registeredCount =
            await _repo.CountRegisteredAsync(
                dto.PeriodId,
                dto.ShiftId,
                dto.WorkDate);

        var assignedStatus =
            registeredCount < staffSlot
                ? RegisteredStatus
                : WaitlistStatus;

        var registration =
            new CaStaffRegistration
            {
                UserId = dto.UserId,
                PeriodId = dto.PeriodId,
                ShiftId = dto.ShiftId,
                WorkDate = dto.WorkDate,
                Status = assignedStatus,
                RegisteredAt = GetVietnamNow()
            };

        await _repo.Add(registration);

        await transaction.CommitAsync();

        return registration;
    }

    public async Task<IEnumerable<CaStaffRegistration>>
        GetMyScheduleAsync(
            int userId,
            int periodId)
    {
        return await _repo.GetMyRegistrationsAsync(
            userId,
            periodId);
    }

    public async Task<IEnumerable<CaStaffRegistration>>
        GetRegistrationsByPeriodAsync(int periodId)
    {
        return await _repo
            .GetRegistrationsByPeriodAsync(periodId);
    }

    /// <summary>
    /// Endpoint cập nhật trạng thái cũ chỉ được dùng
    /// để hủy phiếu trước khi lịch được công bố.
    ///
    /// Không cho phép đổi trực tiếp WAITLIST thành REGISTERED,
    /// vì việc đó sẽ phá nguyên tắc đăng ký trước được nhận trước.
    /// </summary>
    public async Task UpdateStatusAsync(
        int registrationId,
        string newStatus)
    {
        if (string.IsNullOrWhiteSpace(newStatus))
        {
            throw new ArgumentException(
                "Trạng thái đăng ký không được để trống.");
        }

        var normalizedStatus =
            newStatus.Trim().ToUpperInvariant();

        if (normalizedStatus != CancelledStatus)
        {
            throw new ArgumentException(
                "Không được chuyển thủ công giữa " +
                "REGISTERED và WAITLIST. " +
                "Hệ thống tự quyết định theo thứ tự đăng ký.");
        }

        await using var transaction =
            await _repo.BeginSerializableTransactionAsync();

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

        await CancelAndPromoteAsync(registration);

        await transaction.CommitAsync();
    }

    /// <summary>
    /// Nhân viên tự hủy REGISTERED hoặc WAITLIST
    /// trong thời gian đợt còn mở.
    ///
    /// Khi hủy một REGISTERED, người WAITLIST sớm nhất
    /// được tự động chuyển thành REGISTERED.
    /// </summary>
    public async Task<CaStaffRegistration?>
        CancelRegistrationAsync(
            int id,
            int userId)
    {
        await using var transaction =
            await _repo.BeginSerializableTransactionAsync();

        var registration =
            await _repo.GetbyId(id);

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

        var promotedRegistration =
            await CancelAndPromoteAsync(registration);

        await transaction.CommitAsync();

        return promotedRegistration;
    }

    /// <summary>
    /// Hủy một phiếu và đưa người chờ đầu tiên lên
    /// khi phiếu bị hủy đang giữ vị trí chính thức.
    /// </summary>
    private async Task<CaStaffRegistration?>
        CancelAndPromoteAsync(
            CaStaffRegistration registration)
    {
        var currentStatus =
            registration.Status.Trim().ToUpperInvariant();

        if (currentStatus != RegisteredStatus &&
            currentStatus != WaitlistStatus)
        {
            throw new InvalidOperationException(
                "Ca đăng ký này không thể hủy.");
        }

        var shouldPromoteWaitlist =
            currentStatus == RegisteredStatus;

        registration.Status = CancelledStatus;

        await _repo.Update(registration);

        if (!shouldPromoteWaitlist ||
            registration.PeriodId is not int periodId)
        {
            return null;
        }

        var oldestWaitlist =
            await _repo.GetOldestWaitlistAsync(
                periodId,
                registration.ShiftId,
                registration.WorkDate);

        if (oldestWaitlist == null)
        {
            return null;
        }

        oldestWaitlist.Status = RegisteredStatus;

        await _repo.Update(oldestWaitlist);

        return oldestWaitlist;
    }
}
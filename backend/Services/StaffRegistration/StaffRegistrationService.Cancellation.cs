using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Repositories;

namespace LuanVanTotNghiep.Services;

public partial class StaffRegistrationService
{
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


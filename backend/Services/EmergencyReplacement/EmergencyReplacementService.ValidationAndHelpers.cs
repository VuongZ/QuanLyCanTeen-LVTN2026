using System.Globalization;
using System.Text;
using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Services;

public partial class EmergencyReplacementService
{
private async Task<int>
        ValidateManagerAndBranchAsync(
            int managerId,
            CaFinalSchedule schedule)
    {
        var actor =
            await _repo.GetActorAsync(
                managerId);

        if (actor == null)
        {
            throw new KeyNotFoundException(
                "Không tìm thấy người thực hiện thao tác.");
        }

        var normalizedRole =
            NormalizeText(
                actor.Role?.RoleName);

        var isManager =
            normalizedRole.Contains("MANAGER") ||
            normalizedRole.Contains("QUAN LY");

        if (!isManager)
        {
            throw new InvalidOperationException(
                "Chỉ Quản lý mới được xử lý thay ca.");
        }

        if (actor.BranchId is not int actorBranchId)
        {
            throw new InvalidOperationException(
                "Quản lý chưa được gán chi nhánh.");
        }

        if (schedule.Shift.BranchId is not int
                scheduleBranchId ||
            scheduleBranchId != actorBranchId)
        {
            throw new InvalidOperationException(
                "Bạn không được xử lý lịch của " +
                "chi nhánh khác.");
        }

        return actorBranchId;
    }

    private static void ValidateTargetEmployee(
        CaFinalSchedule schedule)
    {
        if (!IsStaffRole(
                schedule.User.Role?.RoleName))
        {
            throw new InvalidOperationException(
                "Chỉ được thực hiện nghiệp vụ thay ca " +
                "đối với lịch của Nhân viên.");
        }
    }

    private static bool IsStaffRole(
        string? roleName)
    {
        var normalizedRole =
            NormalizeText(
                roleName);

        if (string.IsNullOrWhiteSpace(
                normalizedRole))
        {
            return false;
        }

        return
            !normalizedRole.Contains("ADMIN") &&
            !normalizedRole.Contains("MANAGER") &&
            !normalizedRole.Contains("QUAN LY");
    }

    private static void
        ValidateReplacementSourceStatus(
            CaFinalSchedule schedule)
    {
        var isApprovedLeave =
            string.Equals(
                schedule.Status,
                LeaveApprovedStatus,
                StringComparison.OrdinalIgnoreCase);

        var isAbsent =
            string.Equals(
                schedule.Status,
                AbsentStatus,
                StringComparison.OrdinalIgnoreCase);

        if (!isApprovedLeave &&
            !isAbsent)
        {
            throw new InvalidOperationException(
                "Phải ghi nhận nhân viên nghỉ có phép " +
                "hoặc vắng không phép trước khi chọn người thay.");
        }
    }

    /// <summary>
    /// Kiểm tra hai khoảng thời gian ca có giao nhau.
    ///
    /// Có hỗ trợ ca đi qua 0 giờ.
    /// </summary>
    private static bool TimesOverlap(
        TimeOnly firstStart,
        TimeOnly firstEnd,
        TimeOnly secondStart,
        TimeOnly secondEnd)
    {
        const double minutesPerDay =
            24 * 60;

        var firstStartMinutes =
            firstStart.ToTimeSpan().TotalMinutes;

        var firstEndMinutes =
            firstEnd.ToTimeSpan().TotalMinutes;

        if (firstEndMinutes <=
            firstStartMinutes)
        {
            firstEndMinutes +=
                minutesPerDay;
        }

        var secondStartMinutes =
            secondStart.ToTimeSpan().TotalMinutes;

        var secondEndMinutes =
            secondEnd.ToTimeSpan().TotalMinutes;

        if (secondEndMinutes <=
            secondStartMinutes)
        {
            secondEndMinutes +=
                minutesPerDay;
        }

        var offsets = new[]
        {
            -minutesPerDay,
            0,
            minutesPerDay
        };

        return offsets.Any(offset =>
            firstStartMinutes <
                secondEndMinutes + offset &&
            secondStartMinutes + offset <
                firstEndMinutes);
    }
}


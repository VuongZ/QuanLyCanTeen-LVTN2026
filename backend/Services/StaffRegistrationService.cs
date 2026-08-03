using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Repositories;

namespace LuanVanTotNghiep.Services;

public partial class StaffRegistrationService
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
}

using LuanVanTotNghiep.backend.Models.Entities;

namespace LuanVanTotNghiep.Services;

public static class SalaryWagePolicy
{
    public const decimal SeniorHourlyWage = 27_000m;
    private const int SeniorityMonths = 6;

    public static decimal GetHourlyWage(NsUser user, DateOnly referenceDate)
    {
        var baseWage = user.Role?.HourlyWage ?? 0;
        if (user.HireDate == null || referenceDate < user.HireDate.Value.AddMonths(SeniorityMonths))
            return baseWage;

        return Math.Max(baseWage, SeniorHourlyWage);
    }
}

using LuanVanTotNghiep.backend.Models.Entities;

namespace LuanVanTotNghiep.Services;

public static class SalaryWagePolicy
{
    public const decimal DefaultCoefficient = 1.00m;
    public const decimal SixMonthCoefficient = 1.20m;
    public const decimal TwelveMonthCoefficient = 1.50m;

    public static decimal GetHourlyWage(NsUser user, DateOnly referenceDate)
    {
        var baseWage = user.Role?.HourlyWage ?? 0;
        var coefficient = GetEffectiveSalaryCoefficient(user, referenceDate);

        return baseWage * coefficient;
    }

    public static decimal GetEffectiveSalaryCoefficient(NsUser user, DateOnly referenceDate)
    {
        if (user.SalaryCoefficientIsManual && user.SalaryCoefficient > 0)
            return user.SalaryCoefficient;

        user.SalaryCoefficient = GetSalaryCoefficient(user.HireDate, referenceDate);
        return user.SalaryCoefficient;
    }

    public static decimal GetSalaryCoefficient(DateOnly? hireDate, DateOnly referenceDate)
    {
        if (hireDate == null || referenceDate < hireDate.Value.AddMonths(6))
            return DefaultCoefficient;

        return referenceDate < hireDate.Value.AddMonths(12)
            ? SixMonthCoefficient
            : TwelveMonthCoefficient;
    }
}

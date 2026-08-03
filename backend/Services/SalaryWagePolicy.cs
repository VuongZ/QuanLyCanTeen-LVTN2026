using LuanVanTotNghiep.backend.Models.Entities;

namespace LuanVanTotNghiep.Services;

public static class SalaryWagePolicy
{
    public const string PartTime = "PART_TIME";
    public const string FullTime = "FULL_TIME";
    public const string Maternity = "MATERNITY";
    public const decimal DefaultCoefficient = 1.00m;
    public const decimal SixMonthCoefficient = 1.20m;
    public const decimal TwelveMonthCoefficient = 1.50m;
    public const decimal FullTimeStartingCoefficient = 1.20m;
    public const decimal FullTimeAnnualIncrease = 0.30m;

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

        user.EmploymentType = NormalizeEmploymentType(user.EmploymentType);
        user.SalaryCoefficient = GetSalaryCoefficient(
            user.HireDate,
            referenceDate,
            user.EmploymentType);
        return user.SalaryCoefficient;
    }

    public static decimal GetSalaryCoefficient(DateOnly? hireDate, DateOnly referenceDate)
    {
        return GetSalaryCoefficient(hireDate, referenceDate, PartTime);
    }

    public static decimal GetSalaryCoefficient(
        DateOnly? hireDate,
        DateOnly referenceDate,
        string? employmentType)
    {
        if (IsFullTimeEquivalent(employmentType))
        {
            var completedYears = hireDate == null || referenceDate < hireDate.Value
                ? 0
                : GetCompletedYears(hireDate.Value, referenceDate);

            return FullTimeStartingCoefficient
                + completedYears * FullTimeAnnualIncrease;
        }

        if (hireDate == null || referenceDate < hireDate.Value.AddMonths(6))
            return DefaultCoefficient;

        return referenceDate < hireDate.Value.AddMonths(12)
            ? SixMonthCoefficient
            : TwelveMonthCoefficient;
    }

    public static string NormalizeEmploymentType(string? employmentType)
    {
        var normalized = employmentType?.Trim().ToUpperInvariant();
        return normalized switch
        {
            FullTime => FullTime,
            Maternity => Maternity,
            _ => PartTime
        };
    }

    public static bool IsFullTimeEquivalent(string? employmentType)
    {
        var normalized = NormalizeEmploymentType(employmentType);
        return normalized == FullTime || normalized == Maternity;
    }

    private static int GetCompletedYears(DateOnly hireDate, DateOnly referenceDate)
    {
        var years = referenceDate.Year - hireDate.Year;
        if (referenceDate < hireDate.AddYears(years))
            years--;

        return Math.Max(0, years);
    }
}

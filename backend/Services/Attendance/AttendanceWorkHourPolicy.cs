using LuanVanTotNghiep.backend.Models.Entities;

namespace LuanVanTotNghiep.Services;

public static class AttendanceWorkHourPolicy
{
    public const int MaternityGraceMinutes = 30;

    public static decimal CalculateCreditedHours(
        NsUser user,
        CaFinalSchedule schedule,
        DateTime checkIn,
        DateTime checkOut)
    {
        return CalculateCreditedHours(
            user.EmploymentType,
            schedule.WorkDate,
            schedule.Shift.StartTime,
            schedule.Shift.EndTime,
            checkIn,
            checkOut);
    }

    public static decimal CalculateCreditedHours(
        string? employmentType,
        DateOnly workDate,
        TimeOnly shiftStart,
        TimeOnly shiftEnd,
        DateTime checkIn,
        DateTime checkOut)
    {
        var actualHours = Math.Round(
            (decimal)(checkOut - checkIn).TotalHours,
            2);

        if (actualHours <= 0 ||
            SalaryWagePolicy.NormalizeEmploymentType(employmentType) !=
                SalaryWagePolicy.Maternity)
        {
            return actualHours;
        }

        var scheduledStart = workDate.ToDateTime(shiftStart);
        var scheduledEnd = workDate.ToDateTime(shiftEnd);
        if (shiftEnd <= shiftStart)
            scheduledEnd = scheduledEnd.AddDays(1);

        var isWithinMaternityGrace =
            checkIn <= scheduledStart.AddMinutes(MaternityGraceMinutes) &&
            checkOut >= scheduledEnd.AddMinutes(-MaternityGraceMinutes);

        if (!isWithinMaternityGrace)
            return actualHours;

        var scheduledHours = Math.Round(
            (decimal)(scheduledEnd - scheduledStart).TotalHours,
            2);

        return Math.Max(actualHours, scheduledHours);
    }
}

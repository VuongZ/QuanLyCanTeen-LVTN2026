using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "ADMIN")]
public class DashboardController(AppDbContext context) : ControllerBase
{
    [HttpGet("work-hours-ranking")]
    public async Task<IActionResult> GetWorkHoursRanking(
        [FromQuery] int year,
        [FromQuery] int? month,
        [FromQuery] int? branchId)
    {
        if (year is < 2000 or > 2100)
            return BadRequest(new { message = "Năm thống kê không hợp lệ." });
        if (month is < 1 or > 12)
            return BadRequest(new { message = "Tháng thống kê không hợp lệ." });
        if (branchId <= 0)
            return BadRequest(new { message = "Cơ sở không hợp lệ." });

        var query = context.CaAttendances
            .AsNoTracking()
            .Where(attendance =>
                attendance.CheckInTime != null
                && attendance.CheckOutTime != null
                && attendance.Status != "AUTO_CHECKOUT_PENDING"
                && attendance.Schedule.WorkDate.Year == year
                && (month == null || attendance.Schedule.WorkDate.Month == month)
                && (branchId == null || attendance.Schedule.User.BranchId == branchId)
                && (attendance.Schedule.User.Role == null
                    || (attendance.Schedule.User.Role.RoleName != "ADMIN"
                        && attendance.Schedule.User.Role.RoleName != "MANAGER")));

        var attendances = await query
            .Select(attendance => new
            {
                attendance.Schedule.UserId,
                EmployeeName = attendance.Schedule.User.FullName
                    ?? attendance.Schedule.User.Email
                    ?? attendance.Schedule.User.PhoneNumber
                    ?? $"Nhân viên {attendance.Schedule.UserId}",
                attendance.Schedule.User.BranchId,
                BranchName = attendance.Schedule.User.Branch != null
                    ? attendance.Schedule.User.Branch.Name
                    : null,
                attendance.Schedule.User.EmploymentType,
                attendance.Schedule.WorkDate,
                ShiftStart = attendance.Schedule.Shift.StartTime,
                ShiftEnd = attendance.Schedule.Shift.EndTime,
                CheckInTime = attendance.CheckInTime!.Value,
                CheckOutTime = attendance.CheckOutTime!.Value
            })
            .ToListAsync();

        var ranking = attendances
            .Where(attendance => attendance.CheckOutTime > attendance.CheckInTime)
            .GroupBy(attendance => new
            {
                attendance.UserId,
                attendance.EmployeeName,
                attendance.BranchId,
                attendance.BranchName
            })
            .Select(group => new WorkHoursRankingDto
            {
                UserId = group.Key.UserId,
                EmployeeName = group.Key.EmployeeName,
                BranchId = group.Key.BranchId,
                BranchName = group.Key.BranchName,
                TotalHours = Math.Round(
                    group.Sum(attendance =>
                        AttendanceWorkHourPolicy.CalculateCreditedHours(
                            attendance.EmploymentType,
                            attendance.WorkDate,
                            attendance.ShiftStart,
                            attendance.ShiftEnd,
                            attendance.CheckInTime,
                            attendance.CheckOutTime)),
                    2),
                ShiftCount = group.Count()
            })
            .OrderByDescending(item => item.TotalHours)
            .ThenByDescending(item => item.ShiftCount)
            .ThenBy(item => item.EmployeeName)
            .Take(10)
            .ToList();

        for (var index = 0; index < ranking.Count; index++)
            ranking[index].Rank = index + 1;

        return Ok(ranking);
    }
}

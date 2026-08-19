using LuanVanTotNghiep.backend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Repositories;

public class AttendanceRepo : Repository<CaAttendance>
{
    public AttendanceRepo(AppDbContext context)
        : base(context)
    {
    }

    public override async Task<CaAttendance?> GetbyId(int id)
    {
        return await _dbSet
            .FirstOrDefaultAsync(attendance =>
                attendance.Id == id);
    }

    // Lấy Manager và vai trò.
    public async Task<NsUser?> GetManagerByIdAsync(
        int managerId)
    {
        return await Context.NsUsers
            .AsNoTracking()
            .Include(user => user.Role)
            .FirstOrDefaultAsync(user =>
                user.Id == managerId);
    }

    // Lấy nhân viên cùng vai trò và chi nhánh.
    public async Task<NsUser?> GetEmployeeByIdAsync(
        int employeeId)
    {
        return await Context.NsUsers
            .AsNoTracking()
            .Include(user => user.Role)
            .Include(user => user.Branch)
            .FirstOrDefaultAsync(user =>
                user.Id == employeeId);
    }

    // Lấy thông tin ca làm.
   public async Task<CaShift?> GetShiftByIdAsync(
    int shiftId)
{
    return await Context.CaShifts
        .AsNoTracking()
        .Include(shift => shift.Branch)
        .FirstOrDefaultAsync(shift =>
            shift.Id == shiftId);
}

    // Lấy lịch làm chính thức của nhân viên.
    public async Task<CaFinalSchedule?>
        GetPublishedScheduleAsync(
            int employeeId,
            int shiftId,
            DateOnly workDate)
    {
        return await Context.CaFinalSchedules
            .AsNoTracking()
            .FirstOrDefaultAsync(schedule =>
                schedule.UserId == employeeId &&
                schedule.ShiftId == shiftId &&
                schedule.WorkDate == workDate &&
                schedule.Status == "PUBLISHED");
    }

    // Kiểm tra ca đã có báo cáo kết ca được duyệt.
    public async Task<bool> HasApprovedClosingReportAsync(
        int branchId,
        int shiftId,
        DateOnly workDate)
    {
        return await Context.KhoShiftClosingReports
            .AnyAsync(report =>
                report.BranchId == branchId &&
                report.Schedule != null &&
                report.Schedule.ShiftId == shiftId &&
                report.Schedule.WorkDate == workDate &&
                report.Status == "APPROVED");
    }

    // Lấy dữ liệu điểm danh theo lịch chính thức.
    public async Task<CaAttendance?>
        GetByScheduleIdAsync(int scheduleId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(attendance =>
                attendance.ScheduleId == scheduleId);
    }

    // Lấy bảng lương tháng của nhân viên.
    public async Task<LuongMonthlySalary?>
        GetMonthlySalaryAsync(
            int userId,
            int month,
            int year)
    {
        return await Context.LuongMonthlySalaries
            .FirstOrDefaultAsync(salary =>
                salary.UserId == userId &&
                salary.Month == month &&
                salary.Year == year);
    }

    public async Task<List<CaAttendance>> GetDailyHistoryAsync(
        int branchId,
        DateOnly workDate,
        int? shiftId)
    {
        var query = Context.CaAttendances
            .AsNoTracking()
            .Include(attendance => attendance.Schedule)
                .ThenInclude(schedule => schedule.User)
                    .ThenInclude(user => user.Role)
            .Include(attendance => attendance.Schedule)
                .ThenInclude(schedule => schedule.Shift)
            .Include(attendance => attendance.CheckoutRequests)
            .Where(attendance =>
                attendance.Schedule.WorkDate == workDate &&
                attendance.Schedule.User.BranchId == branchId);

        if (shiftId.HasValue)
        {
            query = query.Where(attendance =>
                attendance.Schedule.ShiftId == shiftId.Value);
        }

        return await query
            .OrderByDescending(attendance => attendance.CheckInTime)
            .ThenByDescending(attendance => attendance.Id)
            .ToListAsync();
    }

    // Chỉ thêm điểm danh vào Context, chưa lưu ngay.
    public void AddAttendance(CaAttendance attendance)
    {
        _dbSet.Add(attendance);
    }

    // Chỉ thêm bảng lương vào Context, chưa lưu ngay.
    public void AddSalary(LuongMonthlySalary salary)
    {
        Context.LuongMonthlySalaries.Add(salary);
    }

    // Lưu toàn bộ thay đổi điểm danh và bảng lương cùng lúc.
    public async Task SaveChangesAsync()
    {
        await Context.SaveChangesAsync();
    }
}

using System.Data;
using LuanVanTotNghiep.backend.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace LuanVanTotNghiep.Repositories;

/// <summary>
/// Truy cập dữ liệu cho nghiệp vụ:
/// - Ghi nhận nghỉ/vắng.
/// - Lấy danh sách WAITLIST.
/// - Điều động nhân viên thay ca khẩn cấp.
/// </summary>
public class EmergencyReplacementRepo
{
    private readonly AppDbContext _context;

    public EmergencyReplacementRepo(
        AppDbContext context)
    {
        _context = context;
    }

    public async Task<IDbContextTransaction>
        BeginSerializableTransactionAsync()
    {
        return await _context.Database
            .BeginTransactionAsync(
                IsolationLevel.Serializable);
    }

    /// <summary>
    /// Lấy người đang thực hiện thao tác và vai trò.
    /// </summary>
    public async Task<NsUser?> GetActorAsync(
        int actorId)
    {
        return await _context.NsUsers
            .AsNoTracking()
            .Include(user => user.Role)
            .FirstOrDefaultAsync(user =>
                user.Id == actorId &&
                user.IsDeleted != true);
    }

    /// <summary>
    /// Lấy lịch cần xử lý.
    ///
    /// Không dùng AsNoTracking vì lịch có thể được
    /// cập nhật thành LEAVE_APPROVED hoặc ABSENT.
    /// </summary>
    public async Task<CaFinalSchedule?>
        GetScheduleForUpdateAsync(
            int scheduleId)
    {
        return await _context.CaFinalSchedules
            .Include(schedule => schedule.User)
                .ThenInclude(user => user.Role)
            .Include(schedule => schedule.Shift)
            .Include(schedule =>
                schedule.CaAttendances)
            .Include(schedule =>
                schedule.ReplacementSchedule)
            .FirstOrDefaultAsync(schedule =>
                schedule.Id == scheduleId);
    }

    /// <summary>
    /// Kiểm tra lịch đã có người thay hay chưa.
    /// </summary>
    public async Task<bool> HasReplacementAsync(
        int scheduleId)
    {
        return await _context.CaFinalSchedules
            .AnyAsync(schedule =>
                schedule.ReplacesScheduleId ==
                scheduleId);
    }

    /// <summary>
    /// Lấy tất cả phiếu WAITLIST phù hợp với
    /// đợt, ngày, ca và chi nhánh.
    /// </summary>
    public async Task<List<CaStaffRegistration>>
        GetWaitlistCandidatesAsync(
            int periodId,
            int shiftId,
            DateOnly workDate,
            int branchId)
    {
        return await _context.CaStaffRegistrations
            .AsNoTracking()
            .Include(registration =>
                registration.User)
                .ThenInclude(user =>
                    user.Role)
            .Where(registration =>
                registration.PeriodId == periodId &&
                registration.ShiftId == shiftId &&
                registration.WorkDate == workDate &&
                registration.Status == "WAITLIST" &&
                registration.User.BranchId ==
                    branchId &&
                registration.User.IsDeleted != true)
            .OrderBy(registration =>
                registration.RegisteredAt)
            .ThenBy(registration =>
                registration.Id)
            .ToListAsync();
    }

    /// <summary>
    /// Lấy phiếu WAITLIST được Manager chọn.
    ///
    /// Không dùng AsNoTracking vì trạng thái phiếu
    /// sẽ đổi thành REPLACEMENT_SELECTED.
    /// </summary>
    public async Task<CaStaffRegistration?>
        GetRegistrationForUpdateAsync(
            int registrationId)
    {
        return await _context.CaStaffRegistrations
            .Include(registration =>
                registration.User)
                .ThenInclude(user =>
                    user.Role)
            .Include(registration =>
                registration.Shift)
            .FirstOrDefaultAsync(registration =>
                registration.Id ==
                registrationId);
    }

    /// <summary>
    /// Lấy lịch PUBLISHED của các nhân viên
    /// trong ngày để kiểm tra trùng thời gian.
    /// </summary>
    public async Task<List<CaFinalSchedule>>
        GetPublishedSchedulesForUsersOnDateAsync(
            List<int> userIds,
            DateOnly workDate)
    {
        if (userIds.Count == 0)
        {
            return new List<CaFinalSchedule>();
        }

        return await _context.CaFinalSchedules
            .AsNoTracking()
            .Include(schedule =>
                schedule.Shift)
            .Where(schedule =>
                userIds.Contains(
                    schedule.UserId) &&
                schedule.WorkDate == workDate &&
                schedule.Status == "PUBLISHED")
            .ToListAsync();
    }

    /// <summary>
    /// Lấy quy tắc lương của chi nhánh để chụp lại
    /// hệ số thay ca tại thời điểm điều động.
    ///
    /// Chưa có quy tắc thì Service dùng mặc định 1.50.
    /// </summary>
    public async Task<LuongSalaryRule?>
        GetSalaryRuleByBranchAsync(
            int branchId)
    {
        return await _context.LuongSalaryRules
            .AsNoTracking()
            .Where(rule =>
                rule.BranchId == branchId)
            .OrderByDescending(rule =>
                rule.Id)
            .FirstOrDefaultAsync();
    }

    public void AddSchedule(
        CaFinalSchedule schedule)
    {
        _context.CaFinalSchedules.Add(
            schedule);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
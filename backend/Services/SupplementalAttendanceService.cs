using System.Globalization;
using System.Text;
using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Services;

public class SupplementalAttendanceService
{
    public const string Pending = "PENDING";
    public const string Approved = "APPROVED";
    public const string Rejected = "REJECTED";

    private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);
    private readonly AppDbContext _context;

    public SupplementalAttendanceService(AppDbContext context) => _context = context;

    public async Task<List<SupplementalAttendanceCandidateDto>> GetCandidatesAsync(
        int managerId,
        DateOnly workDate)
    {
        var manager = await RequireManagerAsync(managerId);
        var today = GetVietnamToday();
        if (workDate > today)
            throw new InvalidOperationException("Không thể chấm công bổ sung cho ngày tương lai.");

        if (manager.BranchId is not int branchId)
            throw new InvalidOperationException("Quản lý chưa được phân công cơ sở.");

        var schedules = await _context.CaFinalSchedules
            .AsNoTracking()
            .Include(s => s.User)
            .Include(s => s.Shift)
            .Include(s => s.CaAttendances)
            .Include(s => s.SupplementalAttendanceRequests)
            .Where(s => s.Status == "PUBLISHED"
                && s.WorkDate == workDate
                && s.User.BranchId == branchId
                && !s.CaAttendances.Any()
                && !s.SupplementalAttendanceRequests.Any(r => r.Status != Rejected))
            .OrderBy(s => s.Shift.StartTime)
            .ThenBy(s => s.User.FullName)
            .ToListAsync();

        return schedules.Select(schedule =>
        {
            var previous = schedule.SupplementalAttendanceRequests
                .Where(r => r.Status == Rejected)
                .OrderByDescending(r => r.UpdatedAt)
                .FirstOrDefault();
            return new SupplementalAttendanceCandidateDto
            {
                ScheduleId = schedule.Id,
                EmployeeId = schedule.UserId,
                EmployeeName = schedule.User.FullName ?? schedule.User.Email ?? $"Nhân viên {schedule.UserId}",
                ShiftId = schedule.ShiftId,
                ShiftName = schedule.Shift.ShiftName,
                StartTime = schedule.Shift.StartTime.ToString("HH:mm"),
                EndTime = schedule.Shift.EndTime.ToString("HH:mm"),
                PreviousRequestId = previous?.Id,
                PreviousCheckInTime = previous == null ? null : AsVietnamLocal(previous.ProposedCheckInTime).ToString("yyyy-MM-ddTHH:mm"),
                PreviousCheckOutTime = previous == null ? null : AsVietnamLocal(previous.ProposedCheckOutTime).ToString("yyyy-MM-ddTHH:mm"),
                PreviousRejectReason = previous?.RejectReason
            };
        }).ToList();
    }

    public async Task SubmitAsync(int managerId, SubmitSupplementalAttendanceDto dto)
    {
        var manager = await RequireManagerAsync(managerId);
        if (manager.BranchId is not int branchId)
            throw new InvalidOperationException("Quản lý chưa được phân công cơ sở.");
        if (dto.Entries is not { Count: > 0 })
            throw new InvalidOperationException("Vui lòng chọn ít nhất một nhân viên.");
        if (dto.Entries.Count > 100)
            throw new InvalidOperationException("Mỗi lần chỉ được gửi tối đa 100 nhân viên.");
        if (dto.Entries.Select(e => e.ScheduleId).Distinct().Count() != dto.Entries.Count)
            throw new InvalidOperationException("Danh sách có ca làm bị trùng.");

        var reason = dto.Reason?.Trim();
        if (reason?.Length > 500)
            throw new InvalidOperationException("Lý do không được vượt quá 500 ký tự.");

        var scheduleIds = dto.Entries.Select(e => e.ScheduleId).ToList();
        var schedules = await _context.CaFinalSchedules
            .Include(s => s.User)
            .Include(s => s.Shift)
            .Include(s => s.CaAttendances)
            .Include(s => s.SupplementalAttendanceRequests)
            .Where(s => scheduleIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id);
        if (schedules.Count != scheduleIds.Count)
            throw new InvalidOperationException("Có ca làm không tồn tại.");

        var today = GetVietnamToday();
        var utcNow = DateTime.UtcNow;
        await using var transaction = await _context.Database.BeginTransactionAsync();

        foreach (var entry in dto.Entries)
        {
            var schedule = schedules[entry.ScheduleId];
            if (schedule.Status != "PUBLISHED")
                throw new InvalidOperationException("Chỉ được chấm công bổ sung cho lịch làm đã công bố.");
            if (schedule.User.BranchId != branchId)
                throw new InvalidOperationException($"{DisplayName(schedule.User)} không thuộc cơ sở của quản lý.");
            if (schedule.WorkDate > today)
                throw new InvalidOperationException("Không thể chấm công bổ sung cho ngày tương lai.");
            if (schedule.CaAttendances.Count != 0)
                throw new InvalidOperationException($"Ca của {DisplayName(schedule.User)} đã có chấm công, không thể chấm công bổ sung.");

            var checkInLocal = DateTime.SpecifyKind(entry.CheckInTime, DateTimeKind.Unspecified);
            if (entry.CheckOutTime is null)
                throw new InvalidOperationException($"Vui lòng nhập giờ ra ca của {DisplayName(schedule.User)}.");

            var checkOutLocal = DateTime.SpecifyKind(entry.CheckOutTime.Value, DateTimeKind.Unspecified);
            if (checkOutLocal <= checkInLocal)
                checkOutLocal = checkOutLocal.AddDays(1);

            var dayStart = schedule.WorkDate.ToDateTime(TimeOnly.MinValue);
            var nowLocal = DateTime.UtcNow.Add(VietnamOffset).AddMinutes(1);
            if (checkInLocal < dayStart || checkInLocal >= dayStart.AddDays(1))
                throw new InvalidOperationException($"Giờ vào ca của {DisplayName(schedule.User)} phải thuộc ngày làm đã chọn.");
            if (checkInLocal > nowLocal)
                throw new InvalidOperationException("Giờ vào ca bổ sung không được nằm trong tương lai.");
            if (checkOutLocal > nowLocal)
                throw new InvalidOperationException("Giờ ra ca bổ sung không được nằm trong tương lai.");
            var duration = checkOutLocal - checkInLocal;
            if (duration <= TimeSpan.Zero)
                throw new InvalidOperationException("Giờ ra ca bổ sung phải sau giờ vào ca.");
            if (duration.TotalHours > 18)
                throw new InvalidOperationException("Thời lượng ca bổ sung không được vượt quá 18 giờ.");

            var existing = schedule.SupplementalAttendanceRequests.SingleOrDefault();
            if (existing != null && existing.Status != Rejected)
                throw new InvalidOperationException($"Ca của {DisplayName(schedule.User)} đã có yêu cầu chấm công bổ sung.");

            if (existing == null)
            {
                existing = new CaSupplementalAttendanceRequest
                {
                    ScheduleId = schedule.Id,
                    RequestedByManagerId = managerId,
                    CreatedAt = utcNow
                };
                _context.CaSupplementalAttendanceRequests.Add(existing);
            }

            existing.RequestedByManagerId = managerId;
            existing.ProposedCheckInTime = checkInLocal;
            existing.ProposedCheckOutTime = checkOutLocal;
            existing.Reason = string.IsNullOrWhiteSpace(reason) ? "Quản lý đề nghị chấm công bổ sung" : reason;
            existing.Status = Pending;
            existing.ReviewedByAdminId = null;
            existing.ReviewedAt = null;
            existing.RejectReason = null;
            existing.UpdatedAt = utcNow;
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task<List<SupplementalAttendanceRequestDto>> GetMineAsync(int managerId)
    {
        await RequireManagerAsync(managerId);
        var requests = await BaseQuery().Where(r => r.RequestedByManagerId == managerId)
            .OrderBy(r => r.Status == Pending ? 0 : 1)
            .ThenByDescending(r => r.UpdatedAt)
            .Take(200)
            .ToListAsync();
        return requests.Select(ToDto).ToList();
    }

    public async Task<List<SupplementalAttendanceRequestDto>> GetForReviewAsync(int adminId)
    {
        await RequireAdminAsync(adminId);
        var requests = await BaseQuery().Where(r => r.Status == Pending)
            .OrderBy(r => r.UpdatedAt)
            .ToListAsync();
        return requests.Select(ToDto).ToList();
    }

    public async Task ApproveAsync(int adminId, int requestId)
    {
        await RequireAdminAsync(adminId);
        await using var transaction = await _context.Database.BeginTransactionAsync();
        var claimed = await _context.CaSupplementalAttendanceRequests
            .Where(r => r.Id == requestId && r.Status == Pending)
            .ExecuteUpdateAsync(update => update
                .SetProperty(r => r.Status, "PROCESSING")
                .SetProperty(r => r.UpdatedAt, DateTime.UtcNow));
        if (claimed == 0)
            throw new InvalidOperationException("Yêu cầu không tồn tại hoặc không còn ở trạng thái chờ duyệt.");

        var request = await BaseQuery(false).FirstOrDefaultAsync(r => r.Id == requestId)
            ?? throw new InvalidOperationException("Không tìm thấy yêu cầu chấm công bổ sung.");
        if (request.Schedule.WorkDate > GetVietnamToday())
            throw new InvalidOperationException("Không thể duyệt chấm công bổ sung cho ngày tương lai.");
        if (request.Schedule.CaAttendances.Count != 0)
            throw new InvalidOperationException("Ca làm đã có chấm công, không thể duyệt yêu cầu bổ sung.");

        var workedHours = AttendanceWorkHourPolicy.CalculateCreditedHours(
            request.Schedule.User,
            request.Schedule,
            request.ProposedCheckInTime,
            request.ProposedCheckOutTime);
        if (workedHours <= 0 || workedHours > 18)
            throw new InvalidOperationException("Thời lượng làm việc bổ sung không hợp lệ.");

        var employee = request.Schedule.User;
        var workDate = request.Schedule.WorkDate;
        var salary = await _context.LuongMonthlySalaries.FirstOrDefaultAsync(s =>
            s.UserId == employee.Id && s.Month == workDate.Month && s.Year == workDate.Year);
        if (salary != null && IsSalaryLocked(salary.Status))
            throw new InvalidOperationException("Bảng lương tháng này đã chốt hoặc thanh toán, không thể cộng chấm công bổ sung.");

        var hourlyWage = SalaryWagePolicy.GetHourlyWage(employee, workDate);
        if (salary == null)
        {
            salary = new LuongMonthlySalary
            {
                UserId = employee.Id,
                Month = workDate.Month,
                Year = workDate.Year,
                TotalHours = 0,
                HourlyWageAtTime = hourlyWage,
                TotalSalary = 0,
                TotalBonus = 0,
                TotalPenalty = 0,
                Status = "PENDING",
                CreatedAt = DateTime.UtcNow
            };
            _context.LuongMonthlySalaries.Add(salary);
        }

        salary.TotalHours += workedHours;
        salary.HourlyWageAtTime = hourlyWage;
        salary.TotalSalary = salary.TotalHours * salary.HourlyWageAtTime
            + (salary.TotalBonus ?? 0) - (salary.TotalPenalty ?? 0);

        _context.CaAttendances.Add(new CaAttendance
        {
            ScheduleId = request.ScheduleId,
            CheckInTime = request.ProposedCheckInTime,
            CheckOutTime = request.ProposedCheckOutTime,
            Status = "Đã CheckOut - Bổ sung",
            Salary = salary
        });
        request.Status = Approved;
        request.ReviewedByAdminId = adminId;
        request.ReviewedAt = DateTime.UtcNow;
        request.UpdatedAt = DateTime.UtcNow;
        request.RejectReason = null;

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task RejectAsync(int adminId, int requestId, string? reason)
    {
        await RequireAdminAsync(adminId);
        var rejectReason = reason?.Trim();
        if (string.IsNullOrWhiteSpace(rejectReason))
            throw new InvalidOperationException("Vui lòng nhập lý do từ chối.");
        if (rejectReason.Length > 500)
            throw new InvalidOperationException("Lý do từ chối không được vượt quá 500 ký tự.");

        var now = DateTime.UtcNow;
        var affected = await _context.CaSupplementalAttendanceRequests
            .Where(r => r.Id == requestId && r.Status == Pending)
            .ExecuteUpdateAsync(update => update
                .SetProperty(r => r.Status, Rejected)
                .SetProperty(r => r.RejectReason, rejectReason)
                .SetProperty(r => r.ReviewedByAdminId, adminId)
                .SetProperty(r => r.ReviewedAt, now)
                .SetProperty(r => r.UpdatedAt, now));
        if (affected == 0)
            throw new InvalidOperationException("Yêu cầu không tồn tại hoặc không còn ở trạng thái chờ duyệt.");
    }

    private IQueryable<CaSupplementalAttendanceRequest> BaseQuery(bool noTracking = true)
    {
        var query = _context.CaSupplementalAttendanceRequests
            .Include(r => r.Schedule).ThenInclude(s => s.User).ThenInclude(u => u.Branch)
            .Include(r => r.Schedule).ThenInclude(s => s.Shift)
            .Include(r => r.Schedule).ThenInclude(s => s.CaAttendances)
            .Include(r => r.RequestedByManager)
            .Include(r => r.ReviewedByAdmin)
            .AsQueryable();
        return noTracking ? query.AsNoTracking() : query;
    }

    private async Task<NsUser> RequireManagerAsync(int userId)
    {
        var user = await GetUserAsync(userId);
        var role = Normalize(user.Role?.RoleName);
        if (!role.Contains("MANAGER") && !role.Contains("QUAN LY"))
            throw new UnauthorizedAccessException("Chỉ quản lý mới được gửi chấm công bổ sung.");
        return user;
    }

    private async Task<NsUser> RequireAdminAsync(int userId)
    {
        var user = await GetUserAsync(userId);
        var role = Normalize(user.Role?.RoleName);
        if (!role.Contains("ADMIN") && !role.Contains("QUAN TRI"))
            throw new UnauthorizedAccessException("Chỉ admin mới được duyệt chấm công bổ sung.");
        return user;
    }

    private async Task<NsUser> GetUserAsync(int userId) =>
        await _context.NsUsers.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId && u.IsDeleted != true)
        ?? throw new InvalidOperationException("Không tìm thấy người dùng.");

    private static SupplementalAttendanceRequestDto ToDto(CaSupplementalAttendanceRequest request) => new()
    {
        Id = request.Id,
        ScheduleId = request.ScheduleId,
        EmployeeId = request.Schedule.UserId,
        EmployeeName = DisplayName(request.Schedule.User),
        BranchName = request.Schedule.User.Branch?.Name,
        ShiftName = request.Schedule.Shift.ShiftName,
        WorkDate = request.Schedule.WorkDate.ToString("yyyy-MM-dd"),
        StartTime = request.Schedule.Shift.StartTime.ToString("HH:mm"),
        EndTime = request.Schedule.Shift.EndTime.ToString("HH:mm"),
        ProposedCheckInTime = AsVietnamLocal(request.ProposedCheckInTime),
        ProposedCheckOutTime = AsVietnamLocal(request.ProposedCheckOutTime),
        WorkedHours = AttendanceWorkHourPolicy.CalculateCreditedHours(
            request.Schedule.User,
            request.Schedule,
            request.ProposedCheckInTime,
            request.ProposedCheckOutTime),
        Reason = request.Reason,
        Status = request.Status,
        ManagerName = DisplayName(request.RequestedByManager),
        AdminName = request.ReviewedByAdmin == null ? null : DisplayName(request.ReviewedByAdmin),
        RejectReason = request.RejectReason,
        UpdatedAt = ToVietnamFromUtc(request.UpdatedAt)
    };

    private static string DisplayName(NsUser user) => user.FullName ?? user.Email ?? user.PhoneNumber ?? $"Người dùng {user.Id}";
    private static DateOnly GetVietnamToday() => DateOnly.FromDateTime(DateTime.UtcNow.Add(VietnamOffset));
    private static DateTime AsVietnamLocal(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Unspecified);
    private static DateTime ToVietnamFromUtc(DateTime value) => DateTime.SpecifyKind(value.Add(VietnamOffset), DateTimeKind.Unspecified);
    private static bool IsSalaryLocked(string? status) =>
        string.Equals(status, "FINALIZED", StringComparison.OrdinalIgnoreCase)
        || string.Equals(status, "PAID", StringComparison.OrdinalIgnoreCase);

    private static string Normalize(string? value)
    {
        var text = (value ?? "").Normalize(NormalizationForm.FormD);
        return new string(text.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray())
            .Normalize(NormalizationForm.FormC).ToUpperInvariant();
    }
}

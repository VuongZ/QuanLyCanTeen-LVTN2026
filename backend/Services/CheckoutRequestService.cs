using System.Globalization;
using System.Text;
using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Services;

public class CheckoutRequestService
{
    public const string AwaitingEmployee = "AWAITING_EMPLOYEE";
    public const string Pending = "PENDING";
    public const string Approved = "APPROVED";
    public const string Rejected = "REJECTED";
    public const string AutoCheckoutPending = "AUTO_CHECKOUT_PENDING";

    private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);
    private readonly AppDbContext _context;

    public CheckoutRequestService(AppDbContext context) => _context = context;

    public async Task<int> CreateMissingCheckoutRequestsAsync(DateTime utcNow)
    {
        var localToday = DateOnly.FromDateTime(utcNow.Add(VietnamOffset));
        var fromDate = localToday.AddDays(-7);

        var schedules = await _context.CaFinalSchedules
            .Include(s => s.Shift)
            .Include(s => s.CaAttendances)
                .ThenInclude(a => a.CheckoutRequests)
            .Where(s => s.Status == "PUBLISHED" && s.WorkDate >= fromDate && s.WorkDate <= localToday)
            .ToListAsync();

        var created = 0;
        foreach (var schedule in schedules)
        {
            var attendance = schedule.CaAttendances.OrderBy(a => a.Id).FirstOrDefault();
            if (attendance?.CheckInTime == null || attendance.CheckOutTime != null || attendance.CheckoutRequests.Count != 0)
                continue;

            var shiftEndUtc = GetShiftEndUtc(schedule);
            if (utcNow < shiftEndUtc.AddMinutes(30))
                continue;

            attendance.CheckOutTime = shiftEndUtc;
            attendance.Status = AutoCheckoutPending;
            var request = new CaCheckoutRequest
            {
                Attendance = attendance,
                RequestedByUserId = schedule.UserId,
                ProposedCheckOutTime = shiftEndUtc,
                Status = AwaitingEmployee,
                CreatedAt = utcNow,
                UpdatedAt = utcNow
            };
            request.History.Add(new CaCheckoutRequestHistory
            {
                Action = "AUTO_CREATED", Detail = "Checkout tạm được tạo theo giờ kết thúc ca.", CreatedAt = utcNow
            });
            _context.CaCheckoutRequests.Add(request);
            created++;
        }

        if (created > 0)
            await _context.SaveChangesAsync();

        return created;
    }

    public async Task<List<CheckoutRequestDto>> GetMineAsync(int userId)
    {
        var requests = await BaseQuery()
            .Where(r => r.RequestedByUserId == userId)
            .OrderBy(r => r.Status == AwaitingEmployee || r.Status == Rejected ? 0 : 1)
            .ThenByDescending(r => r.UpdatedAt)
            .ToListAsync();
        return requests.Select(ToDto).ToList();
    }

    public async Task SubmitAsync(int userId, int requestId, SubmitCheckoutRequestDto dto)
    {
        var request = await _context.CaCheckoutRequests
            .Include(r => r.Attendance).ThenInclude(a => a.Schedule)
            .FirstOrDefaultAsync(r => r.Id == requestId && r.RequestedByUserId == userId)
            ?? throw new InvalidOperationException("Không tìm thấy yêu cầu checkout của bạn.");

        if (request.Status != AwaitingEmployee && request.Status != Rejected)
            throw new InvalidOperationException("Yêu cầu này đã được gửi hoặc đã xử lý.");

        var requestedUtc = dto.CheckOutTime.HasValue
            ? VietnamLocalToUtc(dto.CheckOutTime.Value)
            : request.ProposedCheckOutTime;
        var checkIn = request.Attendance.CheckInTime
            ?? throw new InvalidOperationException("Ca làm chưa có giờ check-in.");

        if (requestedUtc < checkIn)
            throw new InvalidOperationException("Giờ checkout không được trước giờ check-in.");
        if (requestedUtc > DateTime.UtcNow.AddMinutes(15))
            throw new InvalidOperationException("Giờ checkout không được nằm trong tương lai.");
        if (requestedUtc > checkIn.AddHours(18))
            throw new InvalidOperationException("Thời lượng ca làm không được vượt quá 18 giờ.");

        var reason = dto.Reason?.Trim();
        if (reason?.Length > 500)
            throw new InvalidOperationException("Lý do không được vượt quá 500 ký tự.");
        if (Math.Abs((requestedUtc - request.ProposedCheckOutTime).TotalMinutes) > 1 && string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Vui lòng nhập lý do khi thay đổi giờ checkout tạm.");

        request.RequestedCheckOutTime = requestedUtc;
        request.Reason = string.IsNullOrWhiteSpace(reason) ? "Xác nhận giờ kết thúc ca" : reason;
        request.Status = Pending;
        request.RejectReason = null;
        request.ReviewedAt = null;
        request.ReviewedByUserId = null;
        request.UpdatedAt = DateTime.UtcNow;
        AddHistory(request, userId, "SUBMITTED", $"Giờ đề nghị: {requestedUtc:O}. Lý do: {request.Reason}");
        await _context.SaveChangesAsync();
    }

    public async Task<List<CheckoutRequestDto>> GetForReviewAsync(int reviewerId)
    {
        var reviewer = await GetUserWithRoleAsync(reviewerId);
        var role = Normalize(reviewer.Role?.RoleName);
        IQueryable<CaCheckoutRequest> query = BaseQuery().Where(r => r.Status == Pending);

        var isAdmin = role.Contains("ADMIN") || role.Contains("QUAN TRI");
        if (!isAdmin && (role.Contains("MANAGER") || role.Contains("QUAN LY")))
            query = query.Where(r => r.RequestedByUser.BranchId == reviewer.BranchId && r.RequestedByUserId != reviewerId);
        else if (!isAdmin)
            throw new InvalidOperationException("Bạn không có quyền duyệt yêu cầu checkout.");

        var requests = await query.OrderBy(r => r.UpdatedAt).ToListAsync();
        requests = requests.Where(r =>
        {
            var requesterRole = Normalize(r.RequestedByUser.Role?.RoleName);
            var requesterIsManager = requesterRole.Contains("MANAGER") || requesterRole.Contains("QUAN LY");
            return isAdmin ? requesterIsManager : !requesterIsManager;
        }).ToList();
        return requests.Select(ToDto).ToList();
    }

    public async Task ApproveAsync(int reviewerId, int requestId)
    {
        var request = await GetRequestForReviewAsync(reviewerId, requestId);
        var checkout = request.RequestedCheckOutTime ?? request.ProposedCheckOutTime;
        var checkIn = request.Attendance.CheckInTime
            ?? throw new InvalidOperationException("Ca làm chưa có giờ check-in.");
        var workedHours = Math.Round((decimal)(checkout - checkIn).TotalHours, 2);
        if (workedHours < 0 || workedHours > 18)
            throw new InvalidOperationException("Thời lượng làm việc không hợp lệ.");

        await AddApprovedHoursToSalaryAsync(request, workedHours);
        request.Attendance.CheckOutTime = checkout;
        request.Attendance.Status = "Đã CheckOut";
        request.Status = Approved;
        request.ReviewedByUserId = reviewerId;
        request.ReviewedAt = DateTime.UtcNow;
        request.UpdatedAt = DateTime.UtcNow;
        request.RejectReason = null;
        AddHistory(request, reviewerId, "APPROVED", $"Giờ checkout được duyệt: {checkout:O}");
        await _context.SaveChangesAsync();
    }

    public async Task RejectAsync(int reviewerId, int requestId, string? reason)
    {
        var rejectReason = reason?.Trim();
        if (string.IsNullOrWhiteSpace(rejectReason))
            throw new InvalidOperationException("Vui lòng nhập lý do từ chối.");
        if (rejectReason.Length > 500)
            throw new InvalidOperationException("Lý do từ chối không được vượt quá 500 ký tự.");

        var request = await GetRequestForReviewAsync(reviewerId, requestId);
        request.Status = Rejected;
        request.RejectReason = rejectReason;
        request.ReviewedByUserId = reviewerId;
        request.ReviewedAt = DateTime.UtcNow;
        request.UpdatedAt = DateTime.UtcNow;
        request.Attendance.Status = AutoCheckoutPending;
        AddHistory(request, reviewerId, "REJECTED", rejectReason);
        await _context.SaveChangesAsync();
    }

    private async Task<CaCheckoutRequest> GetRequestForReviewAsync(int reviewerId, int requestId)
    {
        var reviewer = await GetUserWithRoleAsync(reviewerId);
        var request = await BaseQuery(false).FirstOrDefaultAsync(r => r.Id == requestId)
            ?? throw new InvalidOperationException("Không tìm thấy yêu cầu checkout.");
        if (request.Status != Pending)
            throw new InvalidOperationException("Yêu cầu không còn ở trạng thái chờ duyệt.");

        var reviewerRole = Normalize(reviewer.Role?.RoleName);
        var requesterRole = Normalize(request.RequestedByUser.Role?.RoleName);
        var reviewerIsAdmin = reviewerRole.Contains("ADMIN") || reviewerRole.Contains("QUAN TRI");
        var reviewerIsManager = reviewerRole.Contains("MANAGER") || reviewerRole.Contains("QUAN LY");
        var requesterIsManager = requesterRole.Contains("MANAGER") || requesterRole.Contains("QUAN LY");

        var allowed = requesterIsManager
            ? reviewerIsAdmin && reviewer.Id != request.RequestedByUserId
            : reviewerIsManager && reviewer.BranchId == request.RequestedByUser.BranchId && reviewer.Id != request.RequestedByUserId;
        if (!allowed)
            throw new InvalidOperationException("Bạn không có quyền duyệt yêu cầu checkout này.");
        return request;
    }

    private async Task AddApprovedHoursToSalaryAsync(CaCheckoutRequest request, decimal workedHours)
    {
        if (request.Attendance.SalaryId != null)
            throw new InvalidOperationException("Ca làm này đã được cộng vào bảng lương.");

        var schedule = request.Attendance.Schedule;
        var user = request.RequestedByUser;
        var salary = await _context.LuongMonthlySalaries.FirstOrDefaultAsync(s =>
            s.UserId == user.Id && s.Month == schedule.WorkDate.Month && s.Year == schedule.WorkDate.Year);
        if (salary != null && (
            string.Equals(salary.Status, "FINALIZED", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(salary.Status, "ADMIN_FINALIZED", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(salary.Status, "PAID", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Bảng lương tháng này đã chốt hoặc thanh toán, không thể điều chỉnh checkout.");

        var hourlyWage = SalaryWagePolicy.GetHourlyWage(user, schedule.WorkDate);
        if (salary == null)
        {
            salary = new LuongMonthlySalary
            {
                UserId = user.Id, Month = schedule.WorkDate.Month, Year = schedule.WorkDate.Year,
                TotalHours = 0, HourlyWageAtTime = hourlyWage, TotalSalary = 0,
                TotalBonus = 0, TotalPenalty = 0, Status = "PENDING", CreatedAt = DateTime.UtcNow
            };
            _context.LuongMonthlySalaries.Add(salary);
        }
        salary.TotalHours += workedHours;
        salary.HourlyWageAtTime = hourlyWage;
        salary.TotalSalary = salary.TotalHours * salary.HourlyWageAtTime + (salary.TotalBonus ?? 0) - (salary.TotalPenalty ?? 0);
        request.Attendance.Salary = salary;
    }

    private IQueryable<CaCheckoutRequest> BaseQuery(bool noTracking = true)
    {
        var query = _context.CaCheckoutRequests
            .Include(r => r.Attendance).ThenInclude(a => a.Schedule).ThenInclude(s => s.Shift).ThenInclude(s => s.Branch)
            .Include(r => r.RequestedByUser).ThenInclude(u => u.Role)
            .Include(r => r.ReviewedByUser)
            .AsQueryable();
        return noTracking ? query.AsNoTracking() : query;
    }

    private async Task<NsUser> GetUserWithRoleAsync(int id) =>
        await _context.NsUsers.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id)
        ?? throw new InvalidOperationException("Không tìm thấy người dùng.");

    private static CheckoutRequestDto ToDto(CaCheckoutRequest r) => new()
    {
        Id = r.Id, AttendanceId = r.AttendanceId, ScheduleId = r.Attendance.ScheduleId,
        UserId = r.RequestedByUserId, FullName = r.RequestedByUser.FullName,
        RoleName = r.RequestedByUser.Role?.RoleName, BranchName = r.Attendance.Schedule.Shift.Branch?.Name,
        ShiftName = r.Attendance.Schedule.Shift.ShiftName, WorkDate = r.Attendance.Schedule.WorkDate.ToString("yyyy-MM-dd"),
        StartTime = r.Attendance.Schedule.Shift.StartTime.ToString("HH:mm"), EndTime = r.Attendance.Schedule.Shift.EndTime.ToString("HH:mm"),
        CheckInTime = ToVietnam(r.Attendance.CheckInTime), ProposedCheckOutTime = ToVietnam(r.ProposedCheckOutTime)!.Value,
        RequestedCheckOutTime = ToVietnam(r.RequestedCheckOutTime), Reason = r.Reason, Status = r.Status,
        RejectReason = r.RejectReason, ReviewerName = r.ReviewedByUser?.FullName, UpdatedAt = ToVietnam(r.UpdatedAt)!.Value
    };

    private static DateTime GetShiftEndUtc(CaFinalSchedule schedule)
    {
        var localEnd = schedule.WorkDate.ToDateTime(schedule.Shift.EndTime);
        if (schedule.Shift.EndTime < schedule.Shift.StartTime) localEnd = localEnd.AddDays(1);
        return localEnd.Subtract(VietnamOffset);
    }

    private static DateTime VietnamLocalToUtc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Unspecified).Subtract(VietnamOffset);
    private static DateTime? ToVietnam(DateTime? value) => value?.Add(VietnamOffset);

    private static void AddHistory(CaCheckoutRequest request, int actorUserId, string action, string? detail)
    {
        request.History.Add(new CaCheckoutRequestHistory
        {
            ActorUserId = actorUserId, Action = action, Detail = detail, CreatedAt = DateTime.UtcNow
        });
    }

    private static string Normalize(string? value)
    {
        var text = (value ?? "").Normalize(NormalizationForm.FormD);
        return new string(text.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray())
            .Normalize(NormalizationForm.FormC).ToUpperInvariant();
    }

}

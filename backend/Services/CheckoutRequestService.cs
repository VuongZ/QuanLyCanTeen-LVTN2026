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
        var vietnamNow = ToVietnamFromUtc(utcNow);
        var localToday = DateOnly.FromDateTime(vietnamNow);
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

            var shiftEndLocal = GetShiftEndLocal(schedule);
            if (vietnamNow < shiftEndLocal.AddMinutes(30))
                continue;

            attendance.CheckOutTime = shiftEndLocal;
            attendance.Status = AutoCheckoutPending;
            var request = new CaCheckoutRequest
            {
                Attendance = attendance,
                RequestedByUserId = schedule.UserId,
                ProposedCheckOutTime = shiftEndLocal,
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

        var usesLegacyUtc = UsesLegacyUtcAttendance(request);
        var proposedLocal = NormalizeAttendanceTime(
            request.ProposedCheckOutTime,
            usesLegacyUtc);
        var requestedLocal = dto.CheckOutTime.HasValue
            ? AsVietnamLocal(dto.CheckOutTime.Value)
            : proposedLocal;
        var checkInValue = request.Attendance.CheckInTime
            ?? throw new InvalidOperationException("Ca làm chưa có giờ check-in.");
        var checkIn = NormalizeAttendanceTime(
            checkInValue,
            usesLegacyUtc);

        if (requestedLocal < checkIn)
            throw new InvalidOperationException("Giờ checkout không được trước giờ check-in.");
        if (requestedLocal > ToVietnamFromUtc(DateTime.UtcNow).AddMinutes(15))
            throw new InvalidOperationException("Giờ checkout không được nằm trong tương lai.");
        if (requestedLocal > checkIn.AddHours(18))
            throw new InvalidOperationException("Thời lượng ca làm không được vượt quá 18 giờ.");

        var reason = dto.Reason?.Trim();
        if (reason?.Length > 500)
            throw new InvalidOperationException("Lý do không được vượt quá 500 ký tự.");
        if (Math.Abs((requestedLocal - proposedLocal).TotalMinutes) > 1 && string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Vui lòng nhập lý do khi thay đổi giờ checkout tạm.");

        if (usesLegacyUtc)
        {
            request.Attendance.CheckInTime = checkIn;
            request.Attendance.CheckOutTime = proposedLocal;
            request.ProposedCheckOutTime = proposedLocal;
        }

        request.RequestedCheckOutTime = requestedLocal;
        request.Reason = string.IsNullOrWhiteSpace(reason) ? "Xác nhận giờ kết thúc ca" : reason;
        request.Status = Pending;
        request.RejectReason = null;
        request.ReviewedAt = null;
        request.ReviewedByUserId = null;
        request.UpdatedAt = DateTime.UtcNow;
        AddHistory(request, userId, "SUBMITTED", $"Giờ đề nghị: {requestedLocal:O}. Lý do: {request.Reason}");
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
        var usesLegacyUtc = UsesLegacyUtcAttendance(request);
        var checkout = NormalizeAttendanceTime(
            request.RequestedCheckOutTime ?? request.ProposedCheckOutTime,
            usesLegacyUtc);
        var checkInValue = request.Attendance.CheckInTime
            ?? throw new InvalidOperationException("Ca làm chưa có giờ check-in.");
        var checkIn = NormalizeAttendanceTime(
            checkInValue,
            usesLegacyUtc);
        var workedHours = AttendanceWorkHourPolicy.CalculateCreditedHours(
            request.RequestedByUser,
            request.Attendance.Schedule,
            checkIn,
            checkout);
        if (workedHours < 0 || workedHours > 18)
            throw new InvalidOperationException("Thời lượng làm việc không hợp lệ.");

        await AddApprovedHoursToSalaryAsync(request, workedHours);
        request.Attendance.CheckInTime = checkIn;
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

    private static CheckoutRequestDto ToDto(CaCheckoutRequest request)
    {
        var usesLegacyUtc = UsesLegacyUtcAttendance(request);

        return new CheckoutRequestDto
        {
            Id = request.Id,
            AttendanceId = request.AttendanceId,
            ScheduleId = request.Attendance.ScheduleId,
            UserId = request.RequestedByUserId,
            FullName = request.RequestedByUser.FullName,
            RoleName = request.RequestedByUser.Role?.RoleName,
            BranchName = request.Attendance.Schedule.Shift.Branch?.Name,
            ShiftName = request.Attendance.Schedule.Shift.ShiftName,
            WorkDate = request.Attendance.Schedule.WorkDate.ToString("yyyy-MM-dd"),
            StartTime = request.Attendance.Schedule.Shift.StartTime.ToString("HH:mm"),
            EndTime = request.Attendance.Schedule.Shift.EndTime.ToString("HH:mm"),
            CheckInTime = NormalizeAttendanceTime(request.Attendance.CheckInTime, usesLegacyUtc),
            ProposedCheckOutTime = NormalizeAttendanceTime(request.ProposedCheckOutTime, usesLegacyUtc),
            RequestedCheckOutTime = NormalizeAttendanceTime(request.RequestedCheckOutTime, usesLegacyUtc),
            Reason = request.Reason,
            Status = request.Status,
            RejectReason = request.RejectReason,
            ReviewerName = request.ReviewedByUser?.FullName,
            UpdatedAt = ToVietnamFromUtc(request.UpdatedAt)
        };
    }

    private static DateTime GetShiftEndLocal(CaFinalSchedule schedule)
    {
        var localEnd = schedule.WorkDate.ToDateTime(schedule.Shift.EndTime);
        if (schedule.Shift.EndTime < schedule.Shift.StartTime) localEnd = localEnd.AddDays(1);
        return localEnd;
    }

    private static DateTime AsVietnamLocal(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Unspecified);
    private static DateTime? AsVietnamLocal(DateTime? value) => value == null ? null : AsVietnamLocal(value.Value);
    private static DateTime NormalizeAttendanceTime(DateTime value, bool usesLegacyUtc) =>
        AsVietnamLocal(usesLegacyUtc ? value.Add(VietnamOffset) : value);
    private static DateTime? NormalizeAttendanceTime(DateTime? value, bool usesLegacyUtc) =>
        value == null ? null : NormalizeAttendanceTime(value.Value, usesLegacyUtc);
    private static bool UsesLegacyUtcAttendance(CaCheckoutRequest request)
    {
        var expectedLocalEnd = GetShiftEndLocal(request.Attendance.Schedule);
        var legacyUtcEnd = expectedLocalEnd.Subtract(VietnamOffset);

        return Math.Abs(
            (request.ProposedCheckOutTime - legacyUtcEnd).TotalMinutes) <= 1;
    }
    private static DateTime ToVietnamFromUtc(DateTime value) => DateTime.SpecifyKind(value.Add(VietnamOffset), DateTimeKind.Unspecified);
    private static DateTime? ToVietnamFromUtc(DateTime? value) => value == null ? null : ToVietnamFromUtc(value.Value);

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

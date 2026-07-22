using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Globalization;
using System.Text;

namespace LuanVanTotNghiep.Services;

public class StaffRegistrationService
{
    private readonly AppDbContext _context;

    public StaffRegistrationService(AppDbContext context)
    {
        _context = context;
    }

    private static string NormalizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(character);
        }

        return builder
            .ToString()
            .Normalize(NormalizationForm.FormC)
            .ToUpperInvariant();
    }

    private static TimeZoneInfo GetVietnamTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        }
    }

    private static DateOnly GetVietnamToday()
    {
        var vietnamTimeZone = GetVietnamTimeZone();
        var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            vietnamTimeZone);

        return DateOnly.FromDateTime(vietnamNow);
    }

    private static DateTime GetUtcNowForDatabase()
    {
        return DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
    }

    private static DateTime? ToVietnamTime(DateTime? value)
    {
        if (value == null)
            return null;

        var vietnamTimeZone = GetVietnamTimeZone();
        var converted = DateTime.SpecifyKind(
            TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(value.Value, DateTimeKind.Utc),
                vietnamTimeZone),
            DateTimeKind.Unspecified);

        var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            vietnamTimeZone);

        // Một số dữ liệu cũ có thể đã được lưu theo giờ Việt Nam.
        if (converted > vietnamNow.AddMinutes(5))
            return DateTime.SpecifyKind(value.Value, DateTimeKind.Unspecified);

        return converted;
    }

    // NHÂN VIÊN: Đăng ký ca theo nguyên tắc first come - first serve.
    public async Task<CaStaffRegistration> RegisterAsync(RegisterShiftDto dto)
    {
        await using var transaction = await _context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable);

        // 1. Kiểm tra đợt đăng ký.
        var period = await _context.CaSchedulePeriods
            .FirstOrDefaultAsync(p => p.Id == dto.PeriodId);

        if (period == null)
            throw new KeyNotFoundException("Đợt đăng ký không tồn tại.");

        if (!string.Equals(period.Status, "OPEN", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Đợt đăng ký đã khóa hoặc đã công bố.");

        var today = GetVietnamToday();

        if (today >= period.StartDate)
        {
            throw new InvalidOperationException(
                "Đợt đăng ký đã hết thời gian tiếp nhận đăng ký ca.");
        }

        if (dto.WorkDate < period.StartDate || dto.WorkDate > period.EndDate)
        {
            throw new ArgumentException(
                "Ngày đăng ký không nằm trong thời gian của đợt này.");
        }

        // 2. Kiểm tra nhân viên.
        var user = await _context.NsUsers
            .FirstOrDefaultAsync(u => u.Id == dto.UserId);

        if (user == null)
            throw new KeyNotFoundException("Không tìm thấy nhân viên.");

        if (user.BranchId != period.BranchId)
        {
            throw new InvalidOperationException(
                "Bạn chỉ được đăng ký ca làm tại chi nhánh của mình.");
        }

        // 3. Kiểm tra ca làm.
        var shift = await _context.CaShifts
            .FirstOrDefaultAsync(s => s.Id == dto.ShiftId);

        if (shift == null)
            throw new KeyNotFoundException("Không tìm thấy ca làm.");

        if (shift.BranchId != period.BranchId)
        {
            throw new InvalidOperationException(
                "Ca làm không thuộc chi nhánh của đợt đăng ký.");
        }

        // 4. Kiểm tra ca có được mở vào ngày đăng ký hay không.
        var targetDay = dto.WorkDate.DayOfWeek.ToString();

        var config = await _context.CaBranchShiftConfigs
            .FirstOrDefaultAsync(c =>
                c.ShiftId == dto.ShiftId &&
                c.DayOfWeek == targetDay);

        if (config == null || config.MaxStaff <= 0)
        {
            string[] vietnameseDays =
            {
                "Chủ nhật", "Thứ 2", "Thứ 3", "Thứ 4",
                "Thứ 5", "Thứ 6", "Thứ 7"
            };

            var vietnameseDayName = vietnameseDays[(int)dto.WorkDate.DayOfWeek];
            throw new InvalidOperationException(
                $"Ca làm này không mở vào {vietnameseDayName}.");
        }

        // 5. Chống đăng ký trùng.
        var cancelledStatuses = new[]
        {
            "CANCELLED",
            "REJECTED",
            "Từ Chối"
        };

        var isDuplicate = await _context.CaStaffRegistrations
            .AnyAsync(r =>
                r.PeriodId == dto.PeriodId &&
                r.UserId == dto.UserId &&
                r.ShiftId == dto.ShiftId &&
                r.WorkDate == dto.WorkDate &&
                !cancelledStatuses.Contains(r.Status));

        if (isDuplicate)
        {
            throw new InvalidOperationException(
                "Bạn đã đăng ký ca này vào ngày này rồi.");
        }

        // 6. Kiểm tra số lượng Staff được đăng ký.
        // MaxStaff là tổng số người trong ca, trong đó Manager chiếm 1 vị trí.
        var maxStaff = config.MaxStaff.GetValueOrDefault();
        var staffSlot = Math.Max(maxStaff - 1, 0);

        if (staffSlot <= 0)
        {
            throw new InvalidOperationException(
                "Ca làm này chỉ có vị trí cho Quản lý, nhân viên không thể đăng ký.");
        }

        var registeredCount = await _context.CaStaffRegistrations
            .CountAsync(r =>
                r.PeriodId == dto.PeriodId &&
                r.ShiftId == dto.ShiftId &&
                r.WorkDate == dto.WorkDate &&
                !cancelledStatuses.Contains(r.Status));

        if (registeredCount >= staffSlot)
        {
            throw new InvalidOperationException(
                "Ca đã đủ số lượng nhân viên, bạn không thể đăng ký vào ca này.");
        }

        // 7. Lưu đăng ký.
        var registration = new CaStaffRegistration
        {
            UserId = dto.UserId,
            PeriodId = dto.PeriodId,
            ShiftId = dto.ShiftId,
            WorkDate = dto.WorkDate,
            Status = "REGISTERED"
        };

        _context.CaStaffRegistrations.Add(registration);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return registration;
    }

    // NHÂN VIÊN: Xem các ca đã đăng ký trong một đợt.
    public async Task<IEnumerable<CaStaffRegistration>> GetMyScheduleAsync(
        int userId,
        int periodId)
    {
        return await _context.CaStaffRegistrations
            .Include(r => r.Shift)
            .Where(r =>
                r.UserId == userId &&
                r.PeriodId == periodId)
            .OrderBy(r => r.WorkDate)
            .ToListAsync();
    }

    // MANAGER: Lấy danh sách đăng ký của một đợt theo thứ tự đăng ký.
    public async Task<IEnumerable<CaStaffRegistration>> GetRegistrationsByPeriodAsync(
        int periodId)
    {
        return await _context.CaStaffRegistrations
            .Include(r => r.User)
            .Include(r => r.Shift)
            .Where(r => r.PeriodId == periodId)
            .OrderBy(r => r.WorkDate)
            .ThenBy(r => r.Id)
            .ToListAsync();
    }

    // Cập nhật trạng thái một đăng ký khi lịch chưa được công bố.
    public async Task UpdateStatusAsync(int registrationId, string newStatus)
    {
        var registration = await _context.CaStaffRegistrations
            .FindAsync(registrationId);

        if (registration == null)
            throw new KeyNotFoundException("Không tìm thấy phiếu đăng ký này.");

        var period = await _context.CaSchedulePeriods
            .FindAsync(registration.PeriodId);

        if (period == null)
            throw new KeyNotFoundException("Không tìm thấy đợt đăng ký.");

        if (string.Equals(
                period.Status,
                "PUBLISHED",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Lịch đã được công bố nên không thể thay đổi đăng ký.");
        }

        if (string.IsNullOrWhiteSpace(newStatus))
            throw new ArgumentException("Trạng thái đăng ký không được để trống.");

        var normalizedStatus = newStatus.Trim().ToUpperInvariant();

        if (normalizedStatus != "REGISTERED" &&
            normalizedStatus != "CANCELLED")
        {
            throw new ArgumentException("Trạng thái đăng ký không hợp lệ.");
        }

        registration.Status = normalizedStatus;
        await _context.SaveChangesAsync();
    }

    // MANAGER: Công bố lịch làm chính thức.
    public async Task PublishScheduleAsync(PublishScheduleDto dto)
    {
        await using var transaction = await _context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable);

        var period = await _context.CaSchedulePeriods
            .FirstOrDefaultAsync(p => p.Id == dto.PeriodId);

        if (period == null)
            throw new KeyNotFoundException("Không tìm thấy đợt đăng ký này.");

        if (string.Equals(period.Status, "PUBLISHED", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Lịch đã được công bố, không thể công bố lại.");
        }

        if (!string.Equals(period.Status, "CLOSED", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Chỉ được công bố lịch khi đợt đăng ký đã được khóa.");
        }

        // 1. Lấy Manager của chi nhánh.
        var manager = await _context.NsUsers
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u =>
                u.BranchId == period.BranchId &&
                u.Role != null &&
                (
                    u.Role.RoleName.Contains("Manager") ||
                    u.Role.RoleName.Contains("MANAGER") ||
                    u.Role.RoleName.Contains("Quản lý") ||
                    u.Role.RoleName.Contains("Quan ly")
                ));

        if (manager == null)
        {
            throw new InvalidOperationException(
                "Không tìm thấy Quản lý của chi nhánh để thêm vào lịch làm.");
        }

        // 2. Lấy các đăng ký hợp lệ của Staff.
        var cancelledStatuses = new[]
        {
            "CANCELLED",
            "REJECTED",
            "Từ Chối"
        };

        var registrations = await _context.CaStaffRegistrations
            .Where(r =>
                r.PeriodId == dto.PeriodId &&
                !cancelledStatuses.Contains(r.Status))
            .OrderBy(r => r.WorkDate)
            .ThenBy(r => r.ShiftId)
            .ThenBy(r => r.Id)
            .ToListAsync();

        // 3. Lấy các ca thuộc chi nhánh.
        var branchShifts = await _context.CaShifts
            .Where(s => s.BranchId == period.BranchId)
            .ToListAsync();

        var branchShiftIds = branchShifts
            .Select(s => s.Id)
            .ToList();

        // 4. Lấy cấu hình ca theo ngày trong tuần.
        var shiftConfigs = await _context.CaBranchShiftConfigs
            .Where(c => branchShiftIds.Contains(c.ShiftId))
            .ToListAsync();

        // 5. Tạo tập khóa của các lịch chính thức cần có.
        var finalScheduleKeys =
            new HashSet<(int UserId, int ShiftId, DateOnly WorkDate)>();

        // 5.1. Thêm Staff có đăng ký hợp lệ.
        foreach (var registration in registrations)
        {
            finalScheduleKeys.Add((
                registration.UserId,
                registration.ShiftId,
                registration.WorkDate));

            registration.Status = "REGISTERED";
        }

        // 5.2. Tự động thêm Manager vào từng ca đang được cấu hình hoạt động.
        var currentDate = period.StartDate;

        while (currentDate <= period.EndDate)
        {
            var dayOfWeek = currentDate.DayOfWeek.ToString();

            foreach (var shift in branchShifts)
            {
                var config = shiftConfigs.FirstOrDefault(c =>
                    c.ShiftId == shift.Id &&
                    c.DayOfWeek == dayOfWeek);

                if (config == null || config.MaxStaff <= 0)
                    continue;

                finalScheduleKeys.Add((manager.Id, shift.Id, currentDate));
            }

            currentDate = currentDate.AddDays(1);
        }

        // 6. Lấy lịch chính thức cũ trong phạm vi chi nhánh và thời gian của đợt.
        var existingSchedules = await _context.CaFinalSchedules
            .Include(s => s.CaAttendances)
            .Where(s =>
                s.WorkDate >= period.StartDate &&
                s.WorkDate <= period.EndDate &&
                branchShiftIds.Contains(s.ShiftId))
            .ToListAsync();

        // 7. Cập nhật hoặc xóa lịch cũ.
        foreach (var schedule in existingSchedules)
        {
            var stillExists = finalScheduleKeys.Contains((
                schedule.UserId,
                schedule.ShiftId,
                schedule.WorkDate));

            if (stillExists)
            {
                schedule.Status = "PUBLISHED";
            }
            else if (schedule.CaAttendances.Any())
            {
                // Không xóa lịch đã phát sinh điểm danh.
                schedule.Status = "DRAFT";
            }
            else
            {
                _context.CaFinalSchedules.Remove(schedule);
            }
        }

        // 8. Thêm các lịch mới chưa tồn tại.
        var existingKeys = existingSchedules
            .Select(s => (s.UserId, s.ShiftId, s.WorkDate))
            .ToHashSet();

        foreach (var key in finalScheduleKeys)
        {
            if (existingKeys.Contains(key))
                continue;

            _context.CaFinalSchedules.Add(new CaFinalSchedule
            {
                UserId = key.UserId,
                ShiftId = key.ShiftId,
                WorkDate = key.WorkDate,
                Status = "PUBLISHED"
            });
        }

        // Chỉ chuyển trạng thái sau khi toàn bộ lịch đã được chuẩn bị thành công.
        period.Status = "PUBLISHED";

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    // MANAGER: Quét QR để ghi nhận điểm danh vào hoặc ra ca.
    public async Task<object> ScanAttendanceAsync(ScanAttendanceDto dto)
    {
        var manager = await _context.NsUsers
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == dto.ManagerId);

        if (manager == null)
            throw new KeyNotFoundException("Không tìm thấy quản lý.");

        var managerRole = NormalizeText(manager.Role?.RoleName);

        if (!managerRole.Contains("MANAGER") && !managerRole.Contains("QUAN LY"))
        {
            throw new InvalidOperationException(
                "Chỉ Manager mới được quét QR điểm danh.");
        }

        var employee = await _context.NsUsers
            .Include(u => u.Role)
            .Include(u => u.Branch)
            .FirstOrDefaultAsync(u => u.Id == dto.EmployeeId);

        if (employee == null)
            throw new KeyNotFoundException("Không tìm thấy nhân viên từ mã QR.");

        if (manager.BranchId == null ||
            employee.BranchId == null ||
            manager.BranchId != employee.BranchId)
        {
            throw new InvalidOperationException(
                "Nhân viên không thuộc cơ sở của Manager.");
        }

        var shift = await _context.CaShifts
            .FirstOrDefaultAsync(s => s.Id == dto.ShiftId);

        if (shift == null)
            throw new KeyNotFoundException("Không tìm thấy ca làm.");

        if (shift.BranchId != null && shift.BranchId != manager.BranchId)
        {
            throw new InvalidOperationException(
                "Ca làm không thuộc cơ sở của Manager.");
        }

        var schedule = await _context.CaFinalSchedules
            .FirstOrDefaultAsync(s =>
                s.UserId == dto.EmployeeId &&
                s.ShiftId == dto.ShiftId &&
                s.WorkDate == dto.WorkDate &&
                s.Status == "PUBLISHED");

        if (schedule == null)
        {
            throw new InvalidOperationException(
                "Nhân viên chưa có lịch làm chính thức cho ngày và ca này.");
        }

        var action = NormalizeText(dto.Action);

        if (action != "CHECKIN" && action != "CHECKOUT")
        {
            throw new ArgumentException(
                "Thao tác điểm danh phải là CHECKIN hoặc CHECKOUT.");
        }

        var scanTime = GetUtcNowForDatabase();

        var attendance = await _context.CaAttendances
            .FirstOrDefaultAsync(a => a.ScheduleId == schedule.Id);

        if (attendance == null)
        {
            attendance = new CaAttendance
            {
                ScheduleId = schedule.Id
            };

            _context.CaAttendances.Add(attendance);
        }

        decimal workedHours = 0;

        if (action == "CHECKIN")
        {
            if (attendance.CheckOutTime != null)
            {
                throw new InvalidOperationException(
                    "Ca này đã điểm danh ra ca, không thể điểm danh vào lại.");
            }

            if (attendance.CheckInTime == null)
                attendance.CheckInTime = scanTime;

            attendance.Status = "Đang Trong Ca Làm";
        }
        else
        {
            if (attendance.CheckInTime == null)
            {
                throw new InvalidOperationException(
                    "Nhân viên chưa điểm danh vào ca này.");
            }

            if (attendance.Status == CheckoutRequestService.AutoCheckoutPending)
                throw new Exception("Ca này đã được checkout tạm. Vui lòng xử lý tại mục Quên checkout.");

            if (attendance.CheckOutTime == null)
            {
                attendance.CheckOutTime = scanTime;
                workedHours = Math.Round(
                    (decimal)(attendance.CheckOutTime.Value - attendance.CheckInTime.Value)
                        .TotalHours,
                    2);

                if (workedHours < 0)
                    throw new InvalidOperationException("Giờ điểm danh ra ca không hợp lệ.");

                var month = schedule.WorkDate.Month;
                var year = schedule.WorkDate.Year;

                var salary = await _context.LuongMonthlySalaries
                    .FirstOrDefaultAsync(s =>
                        s.UserId == employee.Id &&
                        s.Month == month &&
                        s.Year == year);

                var hourlyWage = SalaryWagePolicy.GetHourlyWage(
                    employee,
                    schedule.WorkDate);

                if (salary != null && (
                    string.Equals(salary.Status, "FINALIZED", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(salary.Status, "ADMIN_FINALIZED", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(salary.Status, "PAID", StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidOperationException(
                        "Bảng lương của tháng này đã chốt hoặc thanh toán, không thể cộng thêm giờ làm.");
                }

                if (salary == null)
                {
                    salary = new LuongMonthlySalary
                    {
                        UserId = employee.Id,
                        Month = month,
                        Year = year,
                        TotalHours = 0,
                        HourlyWageAtTime = hourlyWage,
                        TotalSalary = 0,
                        TotalBonus = 0,
                        TotalPenalty = 0,
                        Status = "PENDING",
                        CreatedAt = scanTime
                    };

                    _context.LuongMonthlySalaries.Add(salary);
                }

                salary.TotalHours += workedHours;
                salary.HourlyWageAtTime = hourlyWage;
                salary.TotalSalary =
                    (salary.TotalHours * salary.HourlyWageAtTime)
                    + (salary.TotalBonus ?? 0)
                    - (salary.TotalPenalty ?? 0);

                attendance.Salary = salary;
                attendance.Status = "Đã CheckOut";
            }
            else
            {
                attendance.Status = "Đã CheckOut";
            }
        }

        if (attendance.CheckInTime != null && attendance.CheckOutTime != null)
        {
            workedHours = Math.Round(
                (decimal)(attendance.CheckOutTime.Value - attendance.CheckInTime.Value)
                    .TotalHours,
                2);
        }

        await _context.SaveChangesAsync();

        return new
        {
            scheduleId = schedule.Id,
            attendanceId = attendance.Id,
            employee = new
            {
                employee.Id,
                Username = employee.Email ?? employee.PhoneNumber,
                employee.FullName,
                BranchName = employee.Branch?.Name,
                RoleName = employee.Role?.RoleName
            },
            shift = new
            {
                shift.Id,
                shift.ShiftName,
                shift.StartTime,
                shift.EndTime
            },
            workDate = schedule.WorkDate,
            checkInTime = ToVietnamTime(attendance.CheckInTime),
            checkOutTime = ToVietnamTime(attendance.CheckOutTime),
            workedHours,
            salaryId = attendance.SalaryId,
            attendance.Status
        };
    }

    // NHÂN VIÊN: Hủy ca đã đăng ký khi đợt vẫn còn mở và chưa đến hạn.
    public async Task CancelRegistrationAsync(int id, int userId)
    {
        var registration = await _context.CaStaffRegistrations
            .FindAsync(id);

        if (registration == null)
            throw new KeyNotFoundException("Không tìm thấy phiếu đăng ký này.");

        if (registration.UserId != userId)
        {
            throw new InvalidOperationException(
                "Bạn không có quyền hủy ca của người khác.");
        }

        var period = await _context.CaSchedulePeriods
            .FindAsync(registration.PeriodId);

        if (period == null)
            throw new KeyNotFoundException("Không tìm thấy đợt đăng ký.");

        if (!string.Equals(period.Status, "OPEN", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Đợt đăng ký đã khóa hoặc đã công bố, không thể hủy ca.");
        }

        var today = GetVietnamToday();

        if (today >= period.StartDate)
        {
            throw new InvalidOperationException(
                "Đợt đăng ký đã hết hạn, không thể hủy ca.");
        }

        if (!string.Equals(
                registration.Status,
                "REGISTERED",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Ca đăng ký này không thể hủy.");
        }

        registration.Status = "CANCELLED";
        await _context.SaveChangesAsync();
    }
}

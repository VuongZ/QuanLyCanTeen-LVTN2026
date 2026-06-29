using LuanVanTotNghiep.Models.Entities;
using LuanVanTotNghiep.DTOs;
using Microsoft.EntityFrameworkCore;
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
        if (string.IsNullOrWhiteSpace(value)) return "";
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                builder.Append(c);
        }
        return builder.ToString().Normalize(NormalizationForm.FormC).ToUpperInvariant();
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

    private static DateTime GetUtcNowForDatabase()
    {
        return DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
    }

    private static DateTime? ToVietnamTime(DateTime? value)
    {
        if (value == null) return null;

        var vietnamTimeZone = GetVietnamTimeZone();
        var converted = DateTime.SpecifyKind(
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc), vietnamTimeZone),
            DateTimeKind.Unspecified);
        var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vietnamTimeZone);

        // Some rows may already have been saved as Vietnam local time before this fix.
        if (converted > vietnamNow.AddMinutes(5))
            return DateTime.SpecifyKind(value.Value, DateTimeKind.Unspecified);

        return converted;
    }

public async Task<CaStaffRegistration> RegisterAsync(RegisterShiftDto dto)
{
    // 1. Kiểm tra Đợt đăng ký
    var period = await _context.CaSchedulePeriods.FindAsync(dto.PeriodId);
    if (period == null || period.Status != "OPEN")
        throw new Exception("Đợt đăng ký không tồn tại hoặc đã khóa.");

    if (dto.WorkDate < period.StartDate || dto.WorkDate > period.EndDate)
        throw new Exception("Ngày đăng ký không nằm trong thời gian của đợt này.");

    // 2. Trích xuất tên ngày bằng tiếng Anh để khớp 100% với Database (VD: "Tuesday")
    string targetDay = dto.WorkDate.DayOfWeek.ToString(); 

    // 3. Dò Database xem ca làm này ngày hôm đó có mở không
    var config = await _context.CaBranchShiftConfigs
        .FirstOrDefaultAsync(c => c.ShiftId == dto.ShiftId && c.DayOfWeek == targetDay);

    if (config == null || config.MaxStaff <= 0)
    {
        // Chỉ dùng mảng tiếng Việt ở đây để xuất câu thông báo lỗi cho đẹp
        string[] vnDays = { "Chủ nhật", "Thứ 2", "Thứ 3", "Thứ 4", "Thứ 5", "Thứ 6", "Thứ 7" };
        string vnDayName = vnDays[(int)dto.WorkDate.DayOfWeek];
        
        throw new Exception($"Ca làm này không mở vào {vnDayName}.");
    }

    // 4. Chống đăng ký trùng
    var isDuplicate = await _context.CaStaffRegistrations
        .AnyAsync(r => r.UserId == dto.UserId && r.ShiftId == dto.ShiftId && r.WorkDate == dto.WorkDate);
    
    if (isDuplicate)
        throw new Exception("Bạn đã đăng ký ca này vào ngày này rồi!");

    // 5. Lưu nguyện vọng vào Database
    var registration = new CaStaffRegistration
    {
        UserId = dto.UserId,
        PeriodId = dto.PeriodId,
        ShiftId = dto.ShiftId,
        WorkDate = dto.WorkDate,
        Status = "Chờ Duyệt" 
    };

    _context.CaStaffRegistrations.Add(registration);
    await _context.SaveChangesAsync();

    return registration;
}

   // 4. NHÂN VIÊN: Xem lịch làm việc cá nhân (Lấy toàn bộ trạng thái)
    public async Task<IEnumerable<CaStaffRegistration>> GetMyScheduleAsync(int userId, int periodId)
    {
        return await _context.CaStaffRegistrations
            .Include(r => r.Shift) 
            .Where(r => r.UserId == userId 
                     && r.PeriodId == periodId) // 👉 ĐÃ XÓA ĐIỀU KIỆN "Đã Duyệt" Ở ĐÂY
            .OrderBy(r => r.WorkDate)
            .ToListAsync();
    }

    // 1. MANAGER: Lấy danh sách nguyện vọng của 1 Đợt (Sắp xếp theo thứ tự ai nộp trước đứng trên)
    public async Task<IEnumerable<CaStaffRegistration>> GetRegistrationsByPeriodAsync(int periodId)
    {
        return await _context.CaStaffRegistrations
            .Include(r => r.User) // Kéo theo thông tin User để lấy Tên nhân viên
            .Include(r => r.Shift) // Kéo theo thông tin Ca làm
            .Where(r => r.PeriodId == periodId)
            // Mẹo cực hay: ID tăng dần tự động tương đương với việc ai đăng ký trước đứng trước!
            .OrderBy(r => r.WorkDate)
            .ThenBy(r => r.Id) 
            .ToListAsync();
    }

    // 2. MANAGER: Duyệt hoặc Từ chối 1 người
    public async Task UpdateStatusAsync(int registrationId, string newStatus)
    {
        var registration = await _context.CaStaffRegistrations.FindAsync(registrationId);
        if (registration == null)
            throw new Exception("Không tìm thấy phiếu đăng ký này.");

        // Chỉ cho phép nhập 2 trạng thái này để tránh rác DB
        if (newStatus != "Đã Duyệt" && newStatus != "Từ Chối" && newStatus != "Chờ Duyệt")
            throw new ArgumentException("Trạng thái không hợp lệ.");

        registration.Status = newStatus;
        await _context.SaveChangesAsync();
    }

    // 3. MANAGER: Xuất bản lịch làm việc chính thức (Chốt sổ)
    public async Task PublishScheduleAsync(PublishScheduleDto dto)
    {
        // 1. Chuyển trạng thái của Đợt thành PUBLISHED (Đã chốt)
        var period = await _context.CaSchedulePeriods.FindAsync(dto.PeriodId);
        if (period == null) 
            throw new Exception("Không tìm thấy đợt đăng ký này.");
            
        period.Status = "PUBLISHED";

        // 2. Lấy toàn bộ phiếu đăng ký của đợt này lên
        var allRegistrations = await _context.CaStaffRegistrations
            .Where(r => r.PeriodId == dto.PeriodId)
            .ToListAsync();

        // 3. Quét 1 vòng: Ai có ID nằm trong danh sách gửi lên -> Nhận job. Còn lại -> Rớt.
        foreach (var reg in allRegistrations)
        {
            if (dto.ApprovedRegistrationIds.Contains(reg.Id))
            {
                reg.Status = "Đã Duyệt";
            }
            else
            {
                reg.Status = "Từ Chối";
            }
        }

        var approvedRegistrations = allRegistrations
            .Where(r => dto.ApprovedRegistrationIds.Contains(r.Id))
            .ToList();
        var approvedKeys = approvedRegistrations
            .Select(r => (r.UserId, r.ShiftId, r.WorkDate))
            .ToHashSet();
        var branchShiftIds = await _context.CaShifts
            .Where(s => s.BranchId == period.BranchId)
            .Select(s => s.Id)
            .ToListAsync();
        var existingSchedules = await _context.CaFinalSchedules
            .Include(s => s.CaAttendances)
            .Where(s =>
                s.WorkDate >= period.StartDate &&
                s.WorkDate <= period.EndDate &&
                branchShiftIds.Contains(s.ShiftId))
            .ToListAsync();

        foreach (var schedule in existingSchedules)
        {
            var isApproved = approvedKeys.Contains((schedule.UserId, schedule.ShiftId, schedule.WorkDate));
            if (isApproved)
            {
                schedule.Status = "PUBLISHED";
            }
            else if (schedule.CaAttendances.Any())
            {
                schedule.Status = "DRAFT";
            }
            else
            {
                _context.CaFinalSchedules.Remove(schedule);
            }
        }

        var existingKeys = existingSchedules
            .Select(s => (s.UserId, s.ShiftId, s.WorkDate))
            .ToHashSet();
        foreach (var reg in approvedRegistrations)
        {
            if (existingKeys.Contains((reg.UserId, reg.ShiftId, reg.WorkDate)))
                continue;

            _context.CaFinalSchedules.Add(new CaFinalSchedule
            {
                UserId = reg.UserId,
                ShiftId = reg.ShiftId,
                WorkDate = reg.WorkDate,
                Status = "PUBLISHED"
            });
        }

        // Lưu toàn bộ thay đổi (Cập nhật Status của Period và Registration cùng lúc)
        await _context.SaveChangesAsync();
    }
    // Manager quet QR nhan vien de ghi vao ca_final_schedule va ca_attendance.
    public async Task<object> ScanAttendanceAsync(ScanAttendanceDto dto)
    {
        var manager = await _context.NsUsers
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == dto.ManagerId);
        if (manager == null)
            throw new Exception("Khong tim thay quan ly.");

        var managerRole = NormalizeText(manager.Role?.RoleName);
        if (!managerRole.Contains("MANAGER") && !managerRole.Contains("QUAN LY"))
            throw new Exception("Chi Manager moi duoc quet QR cham cong.");

        var employee = await _context.NsUsers
            .Include(u => u.Role)
            .Include(u => u.Branch)
            .FirstOrDefaultAsync(u => u.Id == dto.EmployeeId);
        if (employee == null)
            throw new Exception("Khong tim thay nhan vien tu ma QR.");

        if (manager.BranchId == null || employee.BranchId == null || manager.BranchId != employee.BranchId)
            throw new Exception("Nhan vien khong thuoc co so cua Manager.");

        var shift = await _context.CaShifts.FirstOrDefaultAsync(s => s.Id == dto.ShiftId);
        if (shift == null)
            throw new Exception("Khong tim thay ca lam.");

        if (shift.BranchId != null && shift.BranchId != manager.BranchId)
            throw new Exception("Ca lam khong thuoc co so cua Manager.");

        var schedule = await _context.CaFinalSchedules
            .FirstOrDefaultAsync(s =>
                s.UserId == dto.EmployeeId &&
                s.ShiftId == dto.ShiftId &&
                s.WorkDate == dto.WorkDate &&
                s.Status == "PUBLISHED");

        if (schedule == null)
            throw new Exception("Nhan vien chua co lich lam chinh thuc cho ngay va ca nay.");

        var action = NormalizeText(dto.Action) == "CHECKOUT" ? "CHECKOUT" : "CHECKIN";
        var scanTime = GetUtcNowForDatabase();
        var attendance = await _context.CaAttendances
            .FirstOrDefaultAsync(a => a.ScheduleId == schedule.Id);

        if (attendance == null)
        {
            attendance = new CaAttendance { ScheduleId = schedule.Id };
            _context.CaAttendances.Add(attendance);
        }

        decimal workedHours = 0;
        if (action == "CHECKIN")
        {
            if (attendance.CheckOutTime != null)
                throw new Exception("Ca nay da check-out, khong the check-in lai.");

            if (attendance.CheckInTime == null)
                attendance.CheckInTime = scanTime;

            attendance.Status = "Đang Trong Ca Làm";
        }
        else
        {
            if (attendance.CheckInTime == null)
                throw new Exception("Nhan vien chua check-in ca nay.");

            if (attendance.CheckOutTime == null)
            {
                attendance.CheckOutTime = scanTime;
                workedHours = Math.Round((decimal)(attendance.CheckOutTime.Value - attendance.CheckInTime.Value).TotalHours, 2);
                if (workedHours < 0)
                    throw new Exception("Gio check-out khong hop le.");

                var checkoutVietnamTime = ToVietnamTime(attendance.CheckOutTime) ?? attendance.CheckOutTime.Value;
                var month = checkoutVietnamTime.Month;
                var year = checkoutVietnamTime.Year;
                var salary = await _context.LuongMonthlySalaries
                    .FirstOrDefaultAsync(s => s.UserId == employee.Id && s.Month == month && s.Year == year);
                var hourlyWage = employee.Role?.HourlyWage ?? 0;

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
                salary.TotalSalary = (salary.TotalHours * salary.HourlyWageAtTime)
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
            workedHours = Math.Round((decimal)(attendance.CheckOutTime.Value - attendance.CheckInTime.Value).TotalHours, 2);

        await _context.SaveChangesAsync();

        return new
        {
            scheduleId = schedule.Id,
            attendanceId = attendance.Id,
            employee = new
            {
                employee.Id,
                employee.Username,
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

    // 5. NHÂN VIÊN: Hủy ca đã đăng ký (Chỉ được hủy khi chưa duyệt)
   public async Task CancelRegistrationAsync(int id, int userId)
{
    var reg = await _context.CaStaffRegistrations.FindAsync(id);
    if (reg == null) 
        throw new Exception("Không tìm thấy phiếu đăng ký này.");

    if (reg.UserId != userId) 
        throw new Exception("Bạn không có quyền xóa ca của người khác.");

    // 👉 RÀO CẢN BỔ SUNG: Chặn hủy ca nếu hệ thống đã KHÓA SỔ
    var period = await _context.CaSchedulePeriods.FindAsync(reg.PeriodId);
    if (period == null || period.Status != "OPEN")
        throw new Exception("Hệ thống đang xét duyệt hoặc đã chốt lịch, không thể hủy ca!");

    if (reg.Status != "Chờ Duyệt") 
        throw new Exception("Ca này đã được quản lý xử lý, không thể hủy.");

    _context.CaStaffRegistrations.Remove(reg);
    await _context.SaveChangesAsync();
}
}

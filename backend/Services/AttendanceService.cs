using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Repositories;
using System.Globalization;
using System.Text;

namespace LuanVanTotNghiep.Services;

public partial class AttendanceService
{
    private readonly AttendanceRepo _repo;
    private readonly ShiftDelegationService _shiftDelegationService;

    public AttendanceService(
        AttendanceRepo repo,
        ShiftDelegationService shiftDelegationService)
    {
        _repo = repo;
        _shiftDelegationService = shiftDelegationService;
    }

    private static string NormalizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Normalize(
            NormalizationForm.FormD);

        var builder = new StringBuilder();

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character)
                != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
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
            return TimeZoneInfo.FindSystemTimeZoneById(
                "SE Asia Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById(
                "Asia/Ho_Chi_Minh");
        }
    }

    private static DateTime GetVietnamNowForDatabase()
    {
        return DateTime.SpecifyKind(
            TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                GetVietnamTimeZone()),
            DateTimeKind.Unspecified);
    }

    private static DateTime? ToVietnamTime(DateTime? value)
    {
        if (value == null)
        {
            return null;
        }

        return DateTime.SpecifyKind(
            value.Value,
            DateTimeKind.Unspecified);
    }

    // Manager quét QR để điểm danh vào hoặc ra ca.
    public async Task<object> ScanAttendanceAsync(
        ScanAttendanceDto dto)
    {
        // 1. Kiểm tra Manager.
        var manager =
            await _repo.GetManagerByIdAsync(
                dto.ManagerId);

        if (manager == null)
        {
            throw new KeyNotFoundException(
                "Không tìm thấy quản lý.");
        }

        var managerRole = NormalizeText(
            manager.Role?.RoleName);

        if (manager.BranchId is not int managerBranchId)
        {
            throw new InvalidOperationException(
                "Quản lý chưa được phân công chi nhánh.");
        }

        // 2. Kiểm tra Nhân viên.
        var employee =
            await _repo.GetEmployeeByIdAsync(
                dto.EmployeeId);

        if (employee == null)
        {
            throw new KeyNotFoundException(
                "Không tìm thấy nhân viên từ mã QR.");
        }

        if (employee.BranchId is not int employeeBranchId ||
            employeeBranchId != managerBranchId)
        {
            throw new InvalidOperationException(
                "Nhân viên không thuộc cơ sở của Manager.");
        }

        // 3. Kiểm tra ca làm.
        var shift =
            await _repo.GetShiftByIdAsync(
                dto.ShiftId);

        if (shift == null)
        {
            throw new KeyNotFoundException(
                "Không tìm thấy ca làm.");
        }

        if (shift.BranchId != null &&
            shift.BranchId != managerBranchId)
        {
            throw new InvalidOperationException(
                "Ca làm không thuộc cơ sở của Manager.");
        }

        var isManager =
            managerRole.Contains("MANAGER") ||
            managerRole.Contains("QUAN LY");
        var hasTemporaryPermission =
            await _shiftDelegationService.HasActivePermissionAsync(
                manager.Id,
                managerBranchId,
                dto.ShiftId,
                dto.WorkDate);
        if (!isManager && !hasTemporaryPermission)
        {
            throw new InvalidOperationException(
                "Bạn không có quyền trưởng ca trong thời gian của ca này.");
        }

        // 4. Kiểm tra lịch làm chính thức.
        var schedule =
            await _repo.GetPublishedScheduleAsync(
                dto.EmployeeId,
                dto.ShiftId,
                dto.WorkDate);

        if (schedule == null)
        {
            throw new InvalidOperationException(
                "Nhân viên chưa có lịch làm chính thức " +
                "cho ngày và ca này.");
        }

        // 5. Kiểm tra thao tác điểm danh.
        var action = NormalizeText(dto.Action);

        if (action != "CHECKIN" &&
            action != "CHECKOUT")
        {
            throw new ArgumentException(
                "Thao tác điểm danh phải là CHECKIN hoặc CHECKOUT.");
        }

        // Cơ sở/ca đã ngừng hoạt động không nhận check-in mới.
        // Checkout của ca đã check-in trước đó vẫn được phép để
        // không làm nhân viên bị mắc kẹt giữa ca.
        if (action == "CHECKIN")
        {
            if (shift.Branch == null || !shift.Branch.IsActive)
            {
                throw new InvalidOperationException(
                    "Cơ sở đã ngừng hoạt động nên không thể check-in ca mới.");
            }

            if (!shift.IsActive)
            {
                throw new InvalidOperationException(
                    "Ca làm đã ngừng hoạt động nên không thể check-in mới.");
            }
        }

        // Checkout chỉ được thực hiện khi báo cáo kết ca đã duyệt.
        if (action == "CHECKOUT")
        {
            var hasApprovedClosingReport =
                await _repo.HasApprovedClosingReportAsync(
                    managerBranchId,
                    dto.ShiftId,
                    dto.WorkDate);

            if (!hasApprovedClosingReport)
            {
                throw new InvalidOperationException(
                    "Ca chưa có báo cáo kết ca được Quản lý duyệt. " +
                    "Vui lòng hoàn tất và duyệt báo cáo kết ca " +
                    "trước khi checkout.");
            }
        }

        var scanTime = GetVietnamNowForDatabase();

        // 6. Lấy hoặc tạo dữ liệu điểm danh.
        var attendance =
            await _repo.GetByScheduleIdAsync(
                schedule.Id);

        if (attendance == null)
        {
            attendance = new CaAttendance
            {
                ScheduleId = schedule.Id
            };

            _repo.AddAttendance(attendance);
        }

        decimal workedHours = 0;

        // 7. Xử lý Check-in.
        if (action == "CHECKIN")
        {
            if (attendance.CheckOutTime != null)
            {
                throw new InvalidOperationException(
                    "Ca này đã điểm danh ra ca, " +
                    "không thể điểm danh vào lại.");
            }

            if (attendance.CheckInTime == null)
            {
                attendance.CheckInTime = scanTime;
            }

            attendance.Status =
                string.Equals(
                    attendance.Status,
                    "Đi muộn",
                    StringComparison.OrdinalIgnoreCase)
                    ? "Đi muộn - Đang Trong Ca Làm"
                    : "Đang Trong Ca Làm";
        }
        else
        {
            // 8. Xử lý Check-out.
            if (attendance.CheckInTime == null)
            {
                throw new InvalidOperationException(
                    "Nhân viên chưa điểm danh vào ca này.");
            }

            if (attendance.Status ==
                CheckoutRequestService.AutoCheckoutPending)
            {
                throw new InvalidOperationException(
                    "Ca này đã được checkout tạm. " +
                    "Vui lòng xử lý tại mục Quên checkout.");
            }

            if (attendance.CheckOutTime == null)
            {
                attendance.CheckOutTime = scanTime;

                workedHours = AttendanceWorkHourPolicy.CalculateCreditedHours(
                    employee.EmploymentType,
                    schedule.WorkDate,
                    shift.StartTime,
                    shift.EndTime,
                    attendance.CheckInTime.Value,
                    attendance.CheckOutTime.Value);

                if (workedHours < 0)
                {
                    throw new InvalidOperationException(
                        "Giờ điểm danh ra ca không hợp lệ.");
                }

                var month = schedule.WorkDate.Month;
                var year = schedule.WorkDate.Year;

                var salary =
                    await _repo.GetMonthlySalaryAsync(
                        employee.Id,
                        month,
                        year);

                var hourlyWage =
                    SalaryWagePolicy.GetHourlyWage(
                        employee,
                        schedule.WorkDate);

                if (salary != null &&
                    IsSalaryLocked(salary.Status))
                {
                    throw new InvalidOperationException(
                        "Bảng lương của tháng này đã chốt " +
                        "hoặc thanh toán, không thể cộng thêm giờ làm.");
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

                    _repo.AddSalary(salary);
                }

                salary.TotalHours += workedHours;
                salary.HourlyWageAtTime = hourlyWage;

                salary.TotalSalary =
                    (salary.TotalHours *
                     salary.HourlyWageAtTime)
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

        if (attendance.CheckInTime != null &&
            attendance.CheckOutTime != null)
        {
            workedHours = AttendanceWorkHourPolicy.CalculateCreditedHours(
                employee.EmploymentType,
                schedule.WorkDate,
                shift.StartTime,
                shift.EndTime,
                attendance.CheckInTime.Value,
                attendance.CheckOutTime.Value);
        }

        // 9. Lưu toàn bộ thay đổi.
        await _repo.SaveChangesAsync();

        if (hasTemporaryPermission)
        {
            await _shiftDelegationService.LogActiveActionAsync(
                manager.Id,
                managerBranchId,
                dto.ShiftId,
                dto.WorkDate,
                action == "CHECKIN"
                    ? "EMPLOYEE_CHECKED_IN"
                    : "EMPLOYEE_CHECKED_OUT",
                $"{employee.FullName} - {action}.");
        }

        return new
        {
            scheduleId = schedule.Id,
            attendanceId = attendance.Id,

            employee = new
            {
                employee.Id,
                Username =
                    employee.Email ??
                    employee.PhoneNumber,
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
            checkInTime =
                ToVietnamTime(attendance.CheckInTime),
            checkOutTime =
                ToVietnamTime(attendance.CheckOutTime),
            workedHours,
            salaryId = attendance.SalaryId,
            attendance.Status
        };
    }

    private static bool IsSalaryLocked(string? status)
    {
        return
            string.Equals(
                status,
                "FINALIZED",
                StringComparison.OrdinalIgnoreCase) ||

            string.Equals(
                status,
                "PAID",
                StringComparison.OrdinalIgnoreCase);
    }
}

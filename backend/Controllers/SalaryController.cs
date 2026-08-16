using System.Security.Claims;
using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SalaryController : ControllerBase
{
    private readonly SalaryService _salaryService;
    private readonly SalaryComplaintService _complaintService;
    private readonly AppDbContext _context;

    public SalaryController(
        SalaryService salaryService,
        SalaryComplaintService complaintService,
        AppDbContext context)
    {
        _salaryService = salaryService;
        _complaintService = complaintService;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        if (!IsAdmin())
            return Forbid();

        return Ok(await _salaryService.GetAllAsync());
    }

    [HttpGet("branch")]
    public async Task<IActionResult> GetBranchSalaries()
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null)
            return Unauthorized(new { message = "Không Xác Định Được Người Dùng" });

        if (!IsManager())
            return Forbid();

        if (currentUser.BranchId == null)
            return BadRequest(new { message = "Tài Khoản Quản Lý Chưa Được Gắn Cơ Sở" });

        var salaries = await _salaryService.GetByBranchAsync(currentUser.BranchId.Value);
        return Ok(salaries);
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUser(int userId)
    {
        var role = User.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.Role)?
            .Value?
            .ToUpperInvariant();

        var userIdClaim = User.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?
            .Value;

        var currentUser = int.TryParse(userIdClaim, out var currentUserId)
            ? await _context.NsUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == currentUserId)
            : null;

        if (currentUser == null)
            return Unauthorized(new { message = "Không Xác Định Được Người Dùng." });

        if (currentUser.Id != userId && role != "MANAGER" && role != "ADMIN")
            return Forbid();

        if (role == "MANAGER")
        {
            if (currentUser.BranchId == null)
                return BadRequest(new { message = "Tài Khoản Quản Lý Chưa Được Gắn Cơ Sở." });

            var targetBranchId = await _context.NsUsers
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => u.BranchId)
                .FirstOrDefaultAsync();

            if (targetBranchId != currentUser.BranchId)
                return Forbid();
        }

        // Nhân viên được xem cả bảng lương PENDING của chính mình.
        // Các quyền xem người khác vẫn được kiểm tra ở phía trên.
        var salaries = await _salaryService.GetByUserAsync(userId);
        return Ok(salaries);
    }

    [HttpPost("{salaryId}/complaints")]
    public async Task<IActionResult> CreateComplaint(
        int salaryId,
        [FromBody] CreateSalaryComplaintDto dto)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null)
            return Unauthorized(new { message = "Không xác định được người dùng." });
        if (IsAdminOrManager())
            return Forbid();

        try
        {
            return Ok(await _complaintService.CreateAsync(
                salaryId,
                currentUser.Id,
                dto));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("complaints/my")]
    public async Task<IActionResult> GetMyComplaints()
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null)
            return Unauthorized(new { message = "Không xác định được người dùng." });
        if (IsAdminOrManager())
            return Forbid();

        return Ok(await _complaintService.GetByUserAsync(currentUser.Id));
    }

    [HttpGet("complaints/branch")]
    public async Task<IActionResult> GetBranchComplaints()
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null)
            return Unauthorized(new { message = "Không xác định được người dùng." });
        if (!IsManager())
            return Forbid();
        if (currentUser.BranchId == null)
            return BadRequest(new { message = "Tài khoản Manager chưa được gắn cơ sở." });

        return Ok(await _complaintService.GetByBranchAsync(currentUser.BranchId.Value));
    }

    [HttpPut("complaints/{complaintId}/resolve")]
    public async Task<IActionResult> ResolveComplaint(
        int complaintId,
        [FromBody] ResolveSalaryComplaintDto dto)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null)
            return Unauthorized(new { message = "Không xác định được người dùng." });
        if (!IsManager())
            return Forbid();
        if (currentUser.BranchId == null)
            return BadRequest(new { message = "Tài khoản Manager chưa được gắn cơ sở." });

        try
        {
            var result = await _complaintService.ResolveAsync(
                complaintId,
                currentUser.BranchId.Value,
                currentUser.Id,
                dto);
            if (result == null)
                return NotFound(new { message = "Không tìm thấy khiếu nại trong cơ sở." });
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("user/{userId}/work-details")]
    public async Task<IActionResult> GetUserWorkDetails(
        int userId,
        [FromQuery] int month,
        [FromQuery] int year)
    {
        if (month < 1 || month > 12)
            return BadRequest(new { message = "Tháng không hợp lệ." });

        if (year < 2000 || year > 2100)
            return BadRequest(new { message = "Năm không hợp lệ." });

        var role = User.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.Role)?
            .Value?
            .ToUpperInvariant();

        var userIdClaim = User.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?
            .Value;

        var currentUser = int.TryParse(userIdClaim, out var currentUserId)
            ? await _context.NsUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == currentUserId)
            : null;

        if (currentUser == null)
        {
            return Unauthorized(new
            {
                message = "Không xác định được người dùng."
            });
        }

        // Nhân viên chỉ được xem dữ liệu của chính mình.
        // Quản lý chỉ được xem dữ liệu của người thuộc cùng cơ sở.
        if (currentUser.Id != userId && role != "MANAGER" && role != "ADMIN")
            return Forbid();

        if (role == "MANAGER")
        {
            if (currentUser.BranchId == null)
            {
                return BadRequest(new
                {
                    message = "Tài khoản Quản lý chưa được gắn cơ sở."
                });
            }

            var targetBranchId = await _context.NsUsers
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => u.BranchId)
                .FirstOrDefaultAsync();

            if (targetBranchId != currentUser.BranchId)
                return Forbid();
        }

        if (currentUser.Id == userId && role != "MANAGER" && role != "ADMIN")
        {
            var salaryIsVisible = await _context.LuongMonthlySalaries
                .AsNoTracking()
                .AnyAsync(s =>
                    s.UserId == userId
                    && s.Month == month
                    && s.Year == year);
            if (!salaryIsVisible)
            {
                return NotFound(new
                {
                    message = "Chưa có bảng lương tạm cho kỳ này."
                });
            }
        }

        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1);

        var targetUser = await _context.NsUsers
            .AsNoTracking()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (targetUser == null)
            return NotFound(new { message = "Không tìm thấy nhân viên." });

        var attendances = await _context.CaAttendances
            .AsNoTracking()
            .Include(a => a.Schedule)
                .ThenInclude(s => s.Shift)
            .Where(a =>
                a.Schedule.UserId == userId &&
                a.Schedule.WorkDate >= startDate &&
                a.Schedule.WorkDate < endDate)
            .OrderBy(a => a.Schedule.WorkDate)
            .ThenBy(a => a.Schedule.Shift.StartTime)
            .ToListAsync();

        var result = attendances.Select(attendance =>
        {
            decimal workedHours = 0;

            if (attendance.CheckInTime.HasValue &&
                attendance.CheckOutTime.HasValue)
            {
                workedHours = AttendanceWorkHourPolicy.CalculateCreditedHours(
                    targetUser,
                    attendance.Schedule,
                    attendance.CheckInTime.Value,
                    attendance.CheckOutTime.Value);
            }

            string displayStatus;

            if (attendance.CheckInTime.HasValue &&
                attendance.CheckOutTime.HasValue)
            {
                displayStatus = "COMPLETED";
            }
            else if (attendance.CheckInTime.HasValue)
            {
                displayStatus = "WORKING";
            }
            else
            {
                displayStatus = "NOT_STARTED";
            }

            var salaryCoefficient =
                SalaryWagePolicy.GetEffectiveSalaryCoefficient(
                    targetUser,
                    attendance.Schedule.WorkDate);

            var hourlyWage =
                (targetUser.Role?.HourlyWage ?? 0) *
                salaryCoefficient;

            return new SalaryWorkDetailDto
            {
                AttendanceId = attendance.Id,
                ScheduleId = attendance.ScheduleId,
                WorkDate = attendance.Schedule.WorkDate,
                ShiftId = attendance.Schedule.ShiftId,
                ShiftName = attendance.Schedule.Shift.ShiftName,
                StartTime = attendance.Schedule.Shift.StartTime.ToString("HH:mm"),
                EndTime = attendance.Schedule.Shift.EndTime.ToString("HH:mm"),
                CheckInTime = attendance.CheckInTime,
                CheckOutTime = attendance.CheckOutTime,
                WorkedHours = workedHours,
                SalaryCoefficient = salaryCoefficient,
                IsWeekend =
                    attendance.Schedule.WorkDate.DayOfWeek is
                        DayOfWeek.Saturday or DayOfWeek.Sunday,
                TotalSalary = workedHours * hourlyWage,
                Status = displayStatus
            };
        }).ToList();

        return Ok(result);
    }

    [HttpGet("user/{userId}/adjustment-history")]
    public async Task<IActionResult> GetAdjustmentHistory(
        int userId,
        [FromQuery] int? month,
        [FromQuery] int? year)
    {
        if (month is < 1 or > 12)
            return BadRequest(new { message = "Tháng không hợp lệ." });
        if (year.HasValue && (year < 2000 || year > 2100))
            return BadRequest(new { message = "Năm không hợp lệ." });

        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null)
            return Unauthorized(new { message = "Không xác định được người dùng." });

        if (currentUser.Id != userId && !IsManager() && !IsAdmin())
            return Forbid();

        if (IsManager())
        {
            if (currentUser.BranchId == null)
                return BadRequest(new { message = "Tài khoản quản lý chưa được gắn cơ sở." });

            var targetBranchId = await _context.NsUsers
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => u.BranchId)
                .FirstOrDefaultAsync();

            if (targetBranchId != currentUser.BranchId)
                return Forbid();
        }

        if (currentUser.Id == userId && !IsManager() && !IsAdmin())
        {
            if (!month.HasValue || !year.HasValue)
                return BadRequest(new { message = "Vui lòng chọn kỳ lương." });

            var salaryIsVisible = await _context.LuongMonthlySalaries
                .AsNoTracking()
                .AnyAsync(s =>
                    s.UserId == userId
                    && s.Month == month.Value
                    && s.Year == year.Value
                    && ((s.Status ?? "").ToUpper() == "FINALIZED"
                        || (s.Status ?? "").ToUpper() == "PAID"));
            if (!salaryIsVisible)
                return NotFound(new { message = "Bảng lương chưa được Manager chốt." });
        }

        return Ok(await _salaryService.GetAdjustmentHistoryAsync(userId, month, year));
    }

    [HttpGet("adjustment-requests/pending")]
    public async Task<IActionResult> GetPendingAdjustmentRequests()
    {
        if (!IsAdmin())
            return Forbid();

        return Ok(await _salaryService.GetPendingAdjustmentRequestsAsync());
    }

    [HttpPut("adjustment-requests/{adjustmentId}/review")]
    public async Task<IActionResult> ReviewAdjustmentRequest(
        int adjustmentId,
        [FromBody] ReviewSalaryAdjustmentDto dto)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null)
            return Unauthorized(new { message = "Không xác định được người dùng." });

        if (!IsAdmin())
            return Forbid();

        try
        {
            var result = await _salaryService.ReviewAdjustmentAsync(
                adjustmentId,
                currentUser.Id,
                dto);

            if (result == null)
                return NotFound(new { message = "Không tìm thấy yêu cầu thưởng/phạt." });

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("branch/{branchId}/period/{year}/{month}/transfer")]
    public async Task<IActionResult> MarkBranchTransferred(int branchId, int year, int month)
    {
        if (!IsAdmin())
            return Forbid();

        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null)
            return Unauthorized(new { message = "Không xác định được người dùng." });

        try
        {
            var result = await _salaryService.MarkBranchTransferredAsync(
                branchId,
                month,
                year,
                currentUser.Id);

            if (result == null)
                return NotFound(new { message = "Không tìm thấy bảng lương của cơ sở trong kỳ này." });

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("rule-adjustments")]
    public async Task<IActionResult> GetRuleAdjustments(
        [FromQuery] int month,
        [FromQuery] int year,
        [FromQuery] int? branchId)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null)
            return Unauthorized(new { message = "Không Xác Địng Được Người Dùng." });

        if (!IsAdminOrManager())
            return Forbid();

        var resolvedBranch = ResolveBranchId(currentUser, branchId);
        if (resolvedBranch == null)
            return BadRequest(new { message = "Vui lòng chọn cơ sở." });

        var result = await _salaryService.GetRuleAdjustmentsAsync(
            resolvedBranch.Value,
            month,
            year);

        return Ok(result);
    }

    [HttpPost("parse-invoice-image")]
    public async Task<IActionResult> ParseInvoiceImage(
        IFormFile file,
        [FromServices] InvoiceOcrService invoiceOcrService)
    {
        try
        {
            var result = await invoiceOcrService.ParseInvoiceImageAsync(file);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [HttpPut("rule")]
    public async Task<IActionResult> UpdateSalaryRule([FromBody] UpdateSalaryRuleDto dto)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null)
            return Unauthorized(new { message = "Không Xác Định Được Người Dùng." });

        if (!IsAdmin())
            return Forbid();

        try
        {
            var result = await _salaryService.UpsertSalaryRuleAsync(dto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("rule-adjustments/apply")]
    public async Task<IActionResult> ApplyRuleAdjustment(
        [FromBody] ApplySalaryRuleDto dto,
        [FromQuery] int? branchId)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null)
            return Unauthorized(new { message = "Không Xác Định Được Người Dùng." });

        if (!IsManager())
            return Forbid();

        var resolvedBranch = ResolveBranchId(currentUser, branchId);
        if (resolvedBranch == null)
            return BadRequest(new { message = "Vui lòng chọn cơ sở." });

        try
        {
            var result = await _salaryService.ApplyRuleAdjustmentAsync(
                resolvedBranch.Value,
                dto);

            if (result == null)
                return NotFound(new { message = "Không Tìm Thấy Nhân Viên Trong Cơ Sở Của Bạn." });

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("rule-adjustments/manual")]
    public async Task<IActionResult> AddManualAdjustment(
        [FromBody] ManualSalaryAdjustmentDto dto,
        [FromQuery] int? branchId)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null)
            return Unauthorized(new { message = "Không Xác Định Được Người Dùng." });

        if (!IsManager())
            return Forbid();

        var resolvedBranch = ResolveBranchId(currentUser, branchId);
        if (resolvedBranch == null)
            return BadRequest(new { message = "Vui lòng chọn cơ sở." });

        try
        {
            var result = await _salaryService.AddManualAdjustmentAsync(
                resolvedBranch.Value,
                currentUser.Id,
                dto);

            if (result == null)
                return NotFound(new { message = "Không Tìm Thấy Nhân Viên Trong Cơ Sở Của Bạn." });

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{salaryId}/pay")]
    public async Task<IActionResult> MarkPaid(int salaryId)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null)
            return Unauthorized(new { message = "Không Xác Định Được Người Dùng." });

        if (!IsManager())
            return Forbid();

        if (currentUser.BranchId == null)
            return BadRequest(new { message = "Tài Khoản Quản Lý Chưa Được Gắn cơ sở." });

        SalaryDto? salary;
        try
        {
            salary = await _salaryService.MarkPaidAsync(
                salaryId,
                currentUser.BranchId.Value);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        if (salary == null)
            return NotFound(new { message = "Không Tìm Thấy Bảng Lương Trong Cơ Sở Của Bạn." });

        return Ok(salary);
    }

    [HttpPut("{salaryId}/finalize")]
    public async Task<IActionResult> FinalizeSalary(int salaryId)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null)
            return Unauthorized(new { message = "Không xác định được người dùng." });

        if (!IsManager())
            return Forbid();

        if (currentUser.BranchId == null)
            return BadRequest(new { message = "Tài khoản quản lý chưa được gắn cơ sở." });

        try
        {
            var salary = await _salaryService.FinalizeAsync(
                salaryId,
                currentUser.BranchId.Value,
                currentUser.Id);

            if (salary == null)
                return NotFound(new { message = "Không tìm thấy bảng lương trong cơ sở của bạn." });

            return Ok(salary);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("branch/period/{year}/{month}/finalize")]
    public async Task<IActionResult> FinalizeBranchPeriod(int year, int month)
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null)
            return Unauthorized(new { message = "Không xác định được người dùng." });
        if (!IsManager())
            return Forbid();
        if (currentUser.BranchId == null)
            return BadRequest(new { message = "Tài khoản Manager chưa được gắn cơ sở." });

        try
        {
            var salaries = await _salaryService.FinalizeBranchPeriodAsync(
                currentUser.BranchId.Value,
                month,
                year,
                currentUser.Id);
            return Ok(salaries);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private bool IsAdmin()
    {
        var role = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
        return string.Equals(role, "ADMIN", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsManager()
    {
        var role = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
        return string.Equals(role, "MANAGER", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsAdminOrManager()
    {
        return IsAdmin() || IsManager();
    }

    private int? ResolveBranchId(NsUser currentUser, int? requestedBranchId)
    {
        if (IsAdmin())
            return requestedBranchId ?? currentUser.BranchId;

        if (currentUser.BranchId == null)
            return null;

        if (requestedBranchId != null &&
            requestedBranchId.Value != currentUser.BranchId.Value)
        {
            return null;
        }

        return currentUser.BranchId.Value;
    }

    private async Task<NsUser?> GetCurrentUserAsync()
    {
        var userIdClaim = User.Claims
            .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?
            .Value;

        return int.TryParse(userIdClaim, out var currentUserId)
            ? await _context.NsUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == currentUserId)
            : null;
    }
}

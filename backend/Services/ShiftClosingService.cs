using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Services
{
    public class ShiftClosingService
    {
        private const string StatusPending = "PENDING";
        private const string StatusApproved = "APPROVED";
        private const string StatusRejected = "REJECTED";



        private readonly AppDbContext _context;

        public ShiftClosingService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ClosingShiftInfoDto?> GetTodayClosingShiftAsync(int staffId)
        {
            var staff = await GetValidStaffAsync(staffId);

            var today = DateOnly.FromDateTime(DateTime.Today);
            var now = DateTime.Now.TimeOfDay;

            var schedules = await _context.CaFinalSchedules
                .AsNoTracking()
                .Include(s => s.Shift)
                .Where(s => s.UserId == staffId)
                .ToListAsync();

            var todaySchedules = schedules
                .Where(s => ToDateOnly(s.WorkDate) == today)
                .Where(s => s.Shift != null)
                .Where(s => s.Shift!.BranchId == staff.BranchId)
                .OrderBy(s => ToTimeSpan(s.Shift!.StartTime))
                .ToList();

            if (todaySchedules.Count == 0)
                return null;

            var selectedSchedule = todaySchedules
                .Where(s => now >= ToTimeSpan(s.Shift!.StartTime))
                .OrderByDescending(s => ToTimeSpan(s.Shift!.StartTime))
                .FirstOrDefault()
                ?? todaySchedules.First();

            var attendance = await _context.CaAttendances
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.ScheduleId == selectedSchedule.Id);

            // Báo cáo thuộc lịch của chính nhân viên hiện tại.
            // Dùng để cho phép nhân viên sửa và gửi lại khi báo cáo của họ bị từ chối.
            var ownReport = await _context.KhoShiftClosingReports
                .AsNoTracking()
                .Where(r => r.ScheduleId == selectedSchedule.Id)
                .Select(r => new
                {
                    r.Id,
                    r.ScheduleId,
                    r.Status,
                    r.RejectReason
                })
                .FirstOrDefaultAsync();

            // Chỉ cho phép một báo cáo đang hoạt động cho cùng cơ sở + ngày + ca.
            // PENDING hoặc APPROVED của bất kỳ nhân viên nào đều khóa quyền gửi của các nhân viên khác.
            // REJECTED không khóa, vì sau khi Manager từ chối thì mọi nhân viên trong ca được gửi lại.
            var activeShiftReport = await _context.KhoShiftClosingReports
                .AsNoTracking()
                .Where(r =>
                    r.BranchId == staff.BranchId!.Value &&
                    r.Schedule != null &&
                    r.Schedule.ShiftId == selectedSchedule.ShiftId &&
                    r.Schedule.WorkDate == selectedSchedule.WorkDate &&
                    (
                        r.Status == StatusPending ||
                        r.Status == StatusApproved
                    )
                )
                .OrderBy(r => r.Status == StatusPending ? 0 : 1)
                .ThenByDescending(r => r.Id)
                .Select(r => new
                {
                    r.Id,
                    r.ScheduleId,
                    r.Status
                })
                .FirstOrDefaultAsync();

            var activeReportIsOwn =
                activeShiftReport?.ScheduleId == selectedSchedule.Id;

            string reportStatus;
            int? reportId;
            string? rejectReason;

            if (activeShiftReport != null)
            {
                reportStatus = NormalizeStatus(
                    activeShiftReport.Status,
                    StatusPending
                );

                // Không trả ID báo cáo của nhân viên khác cho Staff hiện tại.
                reportId = activeReportIsOwn
                    ? activeShiftReport.Id
                    : null;

                rejectReason = null;
            }
            else
            {
                var ownStatus = NormalizeStatus(
                    ownReport?.Status,
                    "NONE"
                );

                if (ownStatus == StatusRejected)
                {
                    reportStatus = StatusRejected;
                    reportId = ownReport?.Id;
                    rejectReason = ownReport?.RejectReason;
                }
                else
                {
                    reportStatus = "NONE";
                    reportId = null;
                    rejectReason = null;
                }
            }

            var alreadyReported = activeShiftReport != null;

            var hasCheckedIn = attendance?.CheckInTime != null;
            var hasCheckedOut = attendance?.CheckOutTime != null;

            var startTime = ToTimeSpan(selectedSchedule.Shift!.StartTime);
            var endTime = ToTimeSpan(selectedSchedule.Shift.EndTime);

            var selectedWorkDate =
                ToDateOnly(selectedSchedule.WorkDate) ?? today;

            // IsShiftEnded chỉ dùng để hiển thị thông tin trên giao diện.
            // Quyền gửi báo cáo không còn phụ thuộc vào giờ bắt đầu hoặc giờ kết thúc ca.
            var (_, shiftEndDateTime) =
                GetShiftDateTimeRange(selectedWorkDate, startTime, endTime);

            var isShiftEnded = DateTime.Now > shiftEndDateTime;

            var isPublished = string.Equals(
                selectedSchedule.Status,
                "PUBLISHED",
                StringComparison.OrdinalIgnoreCase
            );

            // Staff được gửi báo cáo vào bất kỳ thời điểm nào sau khi check-in
            // và trước khi checkout. PENDING/APPROVED của bất kỳ Staff nào
            // trong cùng ca sẽ khóa quyền gửi của toàn bộ Staff còn lại.
            var canSubmit =
                isPublished &&
                hasCheckedIn &&
                !hasCheckedOut &&
                activeShiftReport == null;

            string? submitBlockReason = null;

            if (!isPublished)
            {
                submitBlockReason =
                    "Ca làm chưa được công bố chính thức.";
            }
            else if (activeShiftReport != null &&
                     reportStatus == StatusPending)
            {
                submitBlockReason = activeReportIsOwn
                    ? "Báo cáo của bạn đang chờ Quản lý duyệt."
                    : "Ca này đã có nhân viên khác gửi báo cáo và đang chờ Quản lý duyệt.";
            }
            else if (activeShiftReport != null &&
                     reportStatus == StatusApproved)
            {
                submitBlockReason = activeReportIsOwn
                    ? "Báo cáo của bạn đã được Quản lý duyệt. Bạn có thể checkout."
                    : "Ca này đã có báo cáo được Quản lý duyệt. Bạn có thể checkout.";
            }
            else if (!hasCheckedIn)
            {
                submitBlockReason =
                    "Bạn chưa được điểm danh vào ca này.";
            }
            else if (hasCheckedOut)
            {
                submitBlockReason =
                    "Bạn đã checkout nên không thể gửi báo cáo kết ca.";
            }

            return new ClosingShiftInfoDto
            {
                ScheduleId = selectedSchedule.Id,
                ShiftId = selectedSchedule.ShiftId,
                ShiftName = selectedSchedule.Shift.ShiftName
                    ?? $"Ca #{selectedSchedule.ShiftId}",
                WorkDate = selectedWorkDate.ToString("yyyy-MM-dd"),
                StartTime = FormatTime(startTime),
                EndTime = FormatTime(endTime),
                ReportId = reportId,
                ReportStatus = reportStatus,
                RejectReason = rejectReason,
                AlreadyReported = alreadyReported,
                HasCheckedIn = hasCheckedIn,
                HasCheckedOut = hasCheckedOut,
                IsShiftEnded = isShiftEnded,
                CanSubmit = canSubmit,
                SubmitBlockReason = submitBlockReason
            };
        }

        public async Task<List<ClosingFrontStockItemDto>> GetFrontStockForClosingAsync(int staffId)
        {
            var staff = await GetValidStaffAsync(staffId);

            return await _context.KhoBranchFrontStocks
                .AsNoTracking()
                .Where(f =>
                    f.BranchId == staff.BranchId &&
                    f.Product.IsActive == true
                )
                .OrderBy(f => f.Product.ProductName)
                .Select(f => new ClosingFrontStockItemDto
                {
                    ProductId = f.ProductId,
                    ProductCode = f.Product.ProductCode,
                    ProductName = f.Product.ProductName,
                    Unit = f.Product.Unit,
                    SystemCount = Convert.ToInt32(f.Quantity),
                    ActualCount = Convert.ToInt32(f.Quantity)
                })
                .ToListAsync();
        }

        public async Task<int> SubmitShiftClosingReportAsync(
            int staffId,
            SubmitShiftClosingDto dto)
        {
            var staff = await GetValidStaffAsync(staffId);

            if (dto.ScheduleId <= 0)
                throw new InvalidOperationException("Không tìm thấy ca cần báo cáo kết ca.");

            if (dto.Items == null || dto.Items.Count == 0)
                throw new InvalidOperationException("Báo cáo kết ca chưa có sản phẩm nào.");

            var note = string.IsNullOrWhiteSpace(dto.Note)
                ? null
                : dto.Note.Trim();

            if (note?.Length > 255)
                throw new InvalidOperationException("Ghi chú không được vượt quá 255 ký tự.");

            var schedule = await _context.CaFinalSchedules
                .Include(s => s.Shift)
                .FirstOrDefaultAsync(s =>
                    s.Id == dto.ScheduleId &&
                    s.UserId == staffId
                );

            if (schedule == null)
                throw new InvalidOperationException("Không tìm thấy ca làm chính thức của nhân viên.");

            if (schedule.Shift == null)
                throw new InvalidOperationException("Ca làm không hợp lệ.");

            if (schedule.Shift.BranchId != staff.BranchId)
                throw new InvalidOperationException("Ca làm không thuộc cơ sở của nhân viên.");

            var today = DateOnly.FromDateTime(DateTime.Today);

            if (ToDateOnly(schedule.WorkDate) != today)
                throw new InvalidOperationException("Chỉ được báo cáo kết ca cho ca làm trong ngày hiện tại.");

            if (!string.Equals(
                    schedule.Status,
                    "PUBLISHED",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Ca làm chưa được công bố chính thức.");
            }

            var attendance = await _context.CaAttendances
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.ScheduleId == schedule.Id);

            if (attendance?.CheckInTime == null)
            {
                throw new InvalidOperationException(
                    "Bạn chưa được điểm danh vào ca này nên không thể báo cáo kết ca."
                );
            }

            if (attendance.CheckOutTime != null)
            {
                throw new InvalidOperationException(
                    "Bạn đã checkout nên không thể gửi báo cáo kết ca."
                );
            }

            // Không giới hạn thời điểm gửi báo cáo theo giờ ca.
            // Chỉ cần Staff đã check-in, chưa checkout và ca chưa có
            // báo cáo PENDING hoặc APPROVED của một Staff khác.

            var submittedItems = dto.Items
                .Where(i => i.ProductId > 0)
                .GroupBy(i => i.ProductId)
                .Select(g => new SubmitShiftClosingItemDto
                {
                    ProductId = g.Key,
                    ActualCount = g.Last().ActualCount
                })
                .ToList();

            if (submittedItems.Count == 0)
                throw new InvalidOperationException("Danh sách kiểm kê không hợp lệ.");

            foreach (var item in submittedItems)
            {
                if (item.ActualCount < 0)
                    throw new InvalidOperationException("Số lượng thực tế không được âm.");
            }

            var productIds = submittedItems
                .Select(i => i.ProductId)
                .Distinct()
                .ToList();

            var frontStocks = await _context.KhoBranchFrontStocks
                .Include(f => f.Product)
                .Where(f =>
                    f.BranchId == staff.BranchId &&
                    productIds.Contains(f.ProductId) &&
                    f.Product.IsActive == true
                )
                .ToListAsync();

            if (frontStocks.Count != productIds.Count)
            {
                throw new InvalidOperationException(
                    "Có sản phẩm không tồn tại trong tồn quầy hoặc đã ngừng kinh doanh."
                );
            }

            foreach (var submittedItem in submittedItems)
            {
                var frontStock = frontStocks.First(
                    f => f.ProductId == submittedItem.ProductId
                );

                var systemCount = Convert.ToInt32(frontStock.Quantity);

                if (submittedItem.ActualCount > systemCount)
                {
                    throw new InvalidOperationException(
                        $"Sản phẩm '{frontStock.Product.ProductName}' có số lượng thực tế lớn hơn số lượng hệ thống. " +
                        $"Hệ thống: {systemCount}, thực tế: {submittedItem.ActualCount}."
                    );
                }
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // Khóa bản ghi ca làm chung để hai nhân viên cùng ca không thể
                // đồng thời vượt qua bước kiểm tra và cùng tạo báo cáo PENDING.
                await _context.CaShifts
                    .FromSqlInterpolated(
                        $"SELECT * FROM ca_shift WHERE id = {schedule.ShiftId} FOR UPDATE"
                    )
                    .AsNoTracking()
                    .SingleAsync();

                var activeShiftReport = await _context.KhoShiftClosingReports
                    .AsNoTracking()
                    .Where(r =>
                        r.BranchId == staff.BranchId!.Value &&
                        r.Schedule != null &&
                        r.Schedule.ShiftId == schedule.ShiftId &&
                        r.Schedule.WorkDate == schedule.WorkDate &&
                        (
                            r.Status == StatusPending ||
                            r.Status == StatusApproved
                        )
                    )
                    .OrderBy(r => r.Status == StatusPending ? 0 : 1)
                    .ThenByDescending(r => r.Id)
                    .Select(r => new
                    {
                        r.Id,
                        r.ScheduleId,
                        r.Status
                    })
                    .FirstOrDefaultAsync();

                if (activeShiftReport != null)
                {
                    var activeStatus = NormalizeStatus(
                        activeShiftReport.Status,
                        StatusPending
                    );

                    var isOwnActiveReport =
                        activeShiftReport.ScheduleId == dto.ScheduleId;

                    if (activeStatus == StatusPending)
                    {
                        throw new InvalidOperationException(
                            isOwnActiveReport
                                ? "Báo cáo của bạn đang chờ Quản lý duyệt."
                                : "Ca này đã có nhân viên khác gửi báo cáo và đang chờ Quản lý duyệt."
                        );
                    }

                    throw new InvalidOperationException(
                        isOwnActiveReport
                            ? "Báo cáo của bạn đã được Quản lý duyệt."
                            : "Ca này đã có báo cáo được Quản lý duyệt."
                    );
                }

                var existingReport = await _context.KhoShiftClosingReports
                    .Include(r => r.KhoShiftClosingDetails)
                    .FirstOrDefaultAsync(r => r.ScheduleId == dto.ScheduleId);

                KhoShiftClosingReport report;

                if (existingReport == null)
                {
                    report = new KhoShiftClosingReport
                    {
                        BranchId = staff.BranchId!.Value,
                        UserId = staffId,
                        ScheduleId = dto.ScheduleId,
                        ReportDate = DateTime.Now,
                        Note = note,
                        Status = StatusPending,
                        ReviewedBy = null,
                        ReviewedAt = null,
                        RejectReason = null
                    };

                    _context.KhoShiftClosingReports.Add(report);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    var currentStatus = NormalizeStatus(
                        existingReport.Status,
                        StatusPending
                    );

                    if (currentStatus == StatusPending)
                    {
                        throw new InvalidOperationException(
                            "Báo cáo của ca này đang chờ Quản lý duyệt."
                        );
                    }

                    if (currentStatus == StatusApproved)
                    {
                        throw new InvalidOperationException(
                            "Báo cáo của ca này đã được Quản lý duyệt."
                        );
                    }

                    if (currentStatus != StatusRejected)
                    {
                        throw new InvalidOperationException(
                            "Trạng thái báo cáo hiện tại không hợp lệ."
                        );
                    }

                    // Gửi lại báo cáo bị từ chối: dùng lại cùng bản ghi vì schedule_id là duy nhất.
                    _context.KhoShiftClosingDetails.RemoveRange(
                        existingReport.KhoShiftClosingDetails
                    );

                    existingReport.ReportDate = DateTime.Now;
                    existingReport.Note = note;
                    existingReport.Status = StatusPending;
                    existingReport.ReviewedBy = null;
                    existingReport.ReviewedAt = null;
                    existingReport.RejectReason = null;

                    report = existingReport;

                    // Xóa chi tiết cũ trước để không vướng khóa duy nhất
                    // (report_id, product_id) khi thêm lại dữ liệu mới.
                    await _context.SaveChangesAsync();
                }

                foreach (var submittedItem in submittedItems)
                {
                    var frontStock = frontStocks.First(
                        f => f.ProductId == submittedItem.ProductId
                    );

                    var detail = new KhoShiftClosingDetail
                    {
                        ReportId = report.Id,
                        ProductId = submittedItem.ProductId,
                        SystemCount = Convert.ToInt32(frontStock.Quantity),
                        ActualCount = submittedItem.ActualCount
                    };

                    _context.KhoShiftClosingDetails.Add(detail);
                }

                // Không cập nhật tồn quầy tại đây.
                // Tồn quầy chỉ thay đổi khi Manager duyệt báo cáo.
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return report.Id;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task ApproveReportAsync(int managerId, int reportId)
        {
            var manager = await GetValidManagerAsync(managerId);

            if (reportId <= 0)
                throw new InvalidOperationException("Mã báo cáo không hợp lệ.");

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var report = await _context.KhoShiftClosingReports
                    .Include(r => r.KhoShiftClosingDetails)
                        .ThenInclude(d => d.Product)
                    .FirstOrDefaultAsync(r => r.Id == reportId);

                if (report == null)
                    throw new InvalidOperationException("Không tìm thấy báo cáo kết ca.");

                if (report.BranchId != manager.BranchId)
                {
                    throw new InvalidOperationException(
                        "Bạn không được duyệt báo cáo của cơ sở khác."
                    );
                }

                var currentStatus = NormalizeStatus(
                    report.Status,
                    StatusPending
                );

                if (currentStatus == StatusApproved)
                    throw new InvalidOperationException("Báo cáo này đã được duyệt.");

                if (currentStatus == StatusRejected)
                {
                    throw new InvalidOperationException(
                        "Báo cáo này đã bị từ chối. Nhân viên cần gửi lại trước khi duyệt."
                    );
                }

                if (currentStatus != StatusPending)
                    throw new InvalidOperationException("Trạng thái báo cáo không hợp lệ.");

                if (report.KhoShiftClosingDetails.Count == 0)
                    throw new InvalidOperationException("Báo cáo không có chi tiết kiểm kê.");

                var productIds = report.KhoShiftClosingDetails
                    .Select(d => d.ProductId)
                    .Distinct()
                    .ToList();

                var frontStocks = await _context.KhoBranchFrontStocks
                    .Where(f =>
                        f.BranchId == report.BranchId &&
                        productIds.Contains(f.ProductId)
                    )
                    .ToListAsync();

                if (frontStocks.Count != productIds.Count)
                {
                    throw new InvalidOperationException(
                        "Một số sản phẩm trong báo cáo không còn tồn tại tại quầy."
                    );
                }

                foreach (var detail in report.KhoShiftClosingDetails)
                {
                    var frontStock = frontStocks.First(
                        f => f.ProductId == detail.ProductId
                    );

                    var currentQuantity = Convert.ToInt32(frontStock.Quantity);

                    if (currentQuantity != detail.SystemCount)
                    {
                        var productName =
                            detail.Product?.ProductName ??
                            $"Sản phẩm #{detail.ProductId}";

                        throw new InvalidOperationException(
                            $"Tồn quầy của '{productName}' đã thay đổi sau khi nhân viên gửi báo cáo. " +
                            $"Khi gửi: {detail.SystemCount}, hiện tại: {currentQuantity}. " +
                            "Hãy từ chối báo cáo để nhân viên kiểm kê và gửi lại."
                        );
                    }
                }

                foreach (var detail in report.KhoShiftClosingDetails)
                {
                    var frontStock = frontStocks.First(
                        f => f.ProductId == detail.ProductId
                    );

                    frontStock.Quantity = detail.ActualCount;
                }

                report.Status = StatusApproved;
                report.ReviewedBy = managerId;
                report.ReviewedAt = DateTime.Now;
                report.RejectReason = null;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task RejectReportAsync(
            int managerId,
            int reportId,
            string? reason)
        {
            var manager = await GetValidManagerAsync(managerId);

            if (reportId <= 0)
                throw new InvalidOperationException("Mã báo cáo không hợp lệ.");

            var normalizedReason = string.IsNullOrWhiteSpace(reason)
                ? null
                : reason.Trim();

            if (normalizedReason == null)
                throw new InvalidOperationException("Vui lòng nhập lý do từ chối.");

            if (normalizedReason.Length > 500)
                throw new InvalidOperationException("Lý do từ chối không được vượt quá 500 ký tự.");

            var report = await _context.KhoShiftClosingReports
                .FirstOrDefaultAsync(r => r.Id == reportId);

            if (report == null)
                throw new InvalidOperationException("Không tìm thấy báo cáo kết ca.");

            if (report.BranchId != manager.BranchId)
            {
                throw new InvalidOperationException(
                    "Bạn không được từ chối báo cáo của cơ sở khác."
                );
            }

            var currentStatus = NormalizeStatus(report.Status, StatusPending);

            if (currentStatus == StatusApproved)
                throw new InvalidOperationException("Báo cáo đã được duyệt nên không thể từ chối.");

            if (currentStatus == StatusRejected)
                throw new InvalidOperationException("Báo cáo này đã bị từ chối trước đó.");

            if (currentStatus != StatusPending)
                throw new InvalidOperationException("Trạng thái báo cáo không hợp lệ.");

            report.Status = StatusRejected;
            report.ReviewedBy = managerId;
            report.ReviewedAt = DateTime.Now;
            report.RejectReason = normalizedReason;

            // Từ chối không cập nhật tồn quầy.
            await _context.SaveChangesAsync();
        }

        public async Task<List<ShiftClosingReportListDto>> GetMyReportsAsync(
            int staffId)
        {
            await GetValidStaffAsync(staffId);

            return await _context.KhoShiftClosingReports
                .AsNoTracking()
                .Where(r => r.UserId == staffId)
                .OrderByDescending(r => r.Id)
                .Select(r => new ShiftClosingReportListDto
                {
                    Id = r.Id,
                    BranchId = r.BranchId,
                    BranchName = r.Branch.Name,
                    UserId = r.UserId,
                    StaffName = r.User.FullName ?? string.Empty,
                    ScheduleId = r.ScheduleId,
                    ShiftName = r.Schedule != null && r.Schedule.Shift != null
                        ? r.Schedule.Shift.ShiftName
                        : null,
                    WorkDate = r.Schedule != null
                        ? FormatDate(r.Schedule.WorkDate)
                        : null,
                    ReportDate = FormatDateTime(r.ReportDate),
                    ItemCount = r.KhoShiftClosingDetails.Count,
                    TotalSystemCount = r.KhoShiftClosingDetails.Sum(d => d.SystemCount),
                    TotalActualCount = r.KhoShiftClosingDetails.Sum(d => d.ActualCount),
                    TotalDifference = r.KhoShiftClosingDetails.Sum(
                        d => d.SystemCount - d.ActualCount
                    ),
                    Note = r.Note,
                    Status = r.Status,
                    ReviewedBy = r.ReviewedBy,
                    ReviewerName = r.ReviewedByNavigation != null
                        ? r.ReviewedByNavigation.FullName
                        : null,
                    ReviewedAt = FormatNullableDateTime(r.ReviewedAt),
                    RejectReason = r.RejectReason
                })
                .ToListAsync();
        }

        public async Task<ShiftClosingReportDetailDto?> GetMyReportDetailAsync(
            int staffId,
            int reportId)
        {
            await GetValidStaffAsync(staffId);

            var report = await _context.KhoShiftClosingReports
                .AsNoTracking()
                .Include(r => r.Branch)
                .Include(r => r.User)
                .Include(r => r.ReviewedByNavigation)
                .Include(r => r.Schedule)
                    .ThenInclude(s => s.Shift)
                .Include(r => r.KhoShiftClosingDetails)
                    .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(r =>
                    r.Id == reportId &&
                    r.UserId == staffId
                );

            return report == null ? null : MapReportDetail(report);
        }

        public async Task<List<ShiftClosingReportListDto>>
            GetReportsForManagementAsync(int? branchId)
        {
            var query = _context.KhoShiftClosingReports
                .AsNoTracking()
                .AsQueryable();

            if (branchId.HasValue && branchId.Value > 0)
                query = query.Where(r => r.BranchId == branchId.Value);

            return await query
                .OrderByDescending(r => r.Id)
                .Select(r => new ShiftClosingReportListDto
                {
                    Id = r.Id,
                    BranchId = r.BranchId,
                    BranchName = r.Branch.Name,
                    UserId = r.UserId,
                    StaffName = r.User.FullName ?? string.Empty,
                    ScheduleId = r.ScheduleId,
                    ShiftName = r.Schedule != null && r.Schedule.Shift != null
                        ? r.Schedule.Shift.ShiftName
                        : null,
                    WorkDate = r.Schedule != null
                        ? FormatDate(r.Schedule.WorkDate)
                        : null,
                    ReportDate = FormatDateTime(r.ReportDate),
                    ItemCount = r.KhoShiftClosingDetails.Count,
                    TotalSystemCount = r.KhoShiftClosingDetails.Sum(d => d.SystemCount),
                    TotalActualCount = r.KhoShiftClosingDetails.Sum(d => d.ActualCount),
                    TotalDifference = r.KhoShiftClosingDetails.Sum(
                        d => d.SystemCount - d.ActualCount
                    ),
                    Note = r.Note,
                    Status = r.Status,
                    ReviewedBy = r.ReviewedBy,
                    ReviewerName = r.ReviewedByNavigation != null
                        ? r.ReviewedByNavigation.FullName
                        : null,
                    ReviewedAt = FormatNullableDateTime(r.ReviewedAt),
                    RejectReason = r.RejectReason
                })
                .ToListAsync();
        }

        public async Task<ShiftClosingReportDetailDto?>
            GetReportDetailForManagementAsync(
                int reportId,
                int? branchId)
        {
            var query = _context.KhoShiftClosingReports
                .AsNoTracking()
                .Include(r => r.Branch)
                .Include(r => r.User)
                .Include(r => r.ReviewedByNavigation)
                .Include(r => r.Schedule)
                    .ThenInclude(s => s.Shift)
                .Include(r => r.KhoShiftClosingDetails)
                    .ThenInclude(d => d.Product)
                .AsQueryable();

            if (branchId.HasValue && branchId.Value > 0)
                query = query.Where(r => r.BranchId == branchId.Value);

            var report = await query.FirstOrDefaultAsync(
                r => r.Id == reportId
            );

            return report == null ? null : MapReportDetail(report);
        }

        private static ShiftClosingReportDetailDto MapReportDetail(
            KhoShiftClosingReport report)
        {
            return new ShiftClosingReportDetailDto
            {
                Id = report.Id,
                BranchId = report.BranchId,
                BranchName = report.Branch.Name,
                UserId = report.UserId,
                StaffName = report.User.FullName ?? string.Empty,
                ScheduleId = report.ScheduleId,
                ShiftName = report.Schedule?.Shift?.ShiftName,
                WorkDate = report.Schedule != null
                    ? FormatDate(report.Schedule.WorkDate)
                    : null,
                ReportDate = FormatDateTime(report.ReportDate),
                ItemCount = report.KhoShiftClosingDetails.Count,
                TotalSystemCount = report.KhoShiftClosingDetails.Sum(
                    d => d.SystemCount
                ),
                TotalActualCount = report.KhoShiftClosingDetails.Sum(
                    d => d.ActualCount
                ),
                TotalDifference = report.KhoShiftClosingDetails.Sum(
                    d => d.SystemCount - d.ActualCount
                ),
                Note = report.Note,
                Status = report.Status,
                ReviewedBy = report.ReviewedBy,
                ReviewerName = report.ReviewedByNavigation?.FullName,
                ReviewedAt = FormatNullableDateTime(report.ReviewedAt),
                RejectReason = report.RejectReason,
                Items = report.KhoShiftClosingDetails
                    .Select(d => new ShiftClosingReportItemDto
                    {
                        ProductId = d.ProductId,
                        ProductCode = d.Product.ProductCode,
                        ProductName = d.Product.ProductName,
                        Unit = d.Product.Unit,
                        SystemCount = d.SystemCount,
                        ActualCount = d.ActualCount,
                        Difference = d.SystemCount - d.ActualCount
                    })
                    .ToList()
            };
        }

        private async Task<NsUser> GetValidStaffAsync(int staffId)
        {
            if (staffId <= 0)
                throw new InvalidOperationException("Không tìm thấy thông tin nhân viên.");

            var staff = await _context.NsUsers
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u =>
                    u.Id == staffId &&
                    u.IsDeleted != true
                );

            if (staff == null)
                throw new InvalidOperationException("Không tìm thấy tài khoản nhân viên.");

            if (!staff.BranchId.HasValue || staff.BranchId.Value <= 0)
                throw new InvalidOperationException("Nhân viên chưa được gán cơ sở.");

            var roleName = staff.Role?.RoleName?.ToUpperInvariant() ?? "";

            var isStaff =
                roleName == "STAFF" ||
                roleName.Contains("NHÂN VIÊN") ||
                roleName.Contains("NHAN VIEN");

            if (!isStaff)
                throw new InvalidOperationException("Chỉ nhân viên mới được báo cáo kết ca.");

            return staff;
        }

        private async Task<NsUser> GetValidManagerAsync(int managerId)
        {
            if (managerId <= 0)
                throw new InvalidOperationException("Không tìm thấy thông tin Quản lý.");

            var manager = await _context.NsUsers
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u =>
                    u.Id == managerId &&
                    u.IsDeleted != true
                );

            if (manager == null)
                throw new InvalidOperationException("Không tìm thấy tài khoản Quản lý.");

            if (!manager.BranchId.HasValue || manager.BranchId.Value <= 0)
                throw new InvalidOperationException("Quản lý chưa được gán cơ sở.");

            var roleName = manager.Role?.RoleName?.ToUpperInvariant() ?? "";

            var isManager =
                roleName == "MANAGER" ||
                roleName.Contains("QUẢN LÝ") ||
                roleName.Contains("QUAN LY");

            if (!isManager)
                throw new InvalidOperationException("Chỉ Quản lý mới được duyệt báo cáo kết ca.");

            return manager;
        }

        private static string NormalizeStatus(
            string? value,
            string fallback)
        {
            return string.IsNullOrWhiteSpace(value)
                ? fallback
                : value.Trim().ToUpperInvariant();
        }

        private static DateOnly? ToDateOnly(object? value)
        {
            if (value == null)
                return null;

            if (value is DateOnly dateOnly)
                return dateOnly;

            if (value is DateTime dateTime)
                return DateOnly.FromDateTime(dateTime);

            if (DateTime.TryParse(value.ToString(), out var parsedDate))
                return DateOnly.FromDateTime(parsedDate);

            return null;
        }

        private static TimeSpan ToTimeSpan(object? value)
        {
            if (value == null)
                return TimeSpan.Zero;

            if (value is TimeOnly timeOnly)
                return timeOnly.ToTimeSpan();

            if (value is TimeSpan timeSpan)
                return timeSpan;

            if (value is DateTime dateTime)
                return dateTime.TimeOfDay;

            if (TimeSpan.TryParse(value.ToString(), out var parsedTime))
                return parsedTime;

            return TimeSpan.Zero;
        }

        private static (DateTime Start, DateTime End) GetShiftDateTimeRange(
            DateOnly workDate,
            TimeSpan startTime,
            TimeSpan endTime)
        {
            var startDateTime = workDate
                .ToDateTime(TimeOnly.MinValue)
                .Add(startTime);

            var endDateTime = workDate
                .ToDateTime(TimeOnly.MinValue)
                .Add(endTime);

            // Hỗ trợ ca qua đêm, ví dụ 22:00 - 06:00.
            if (endTime < startTime)
                endDateTime = endDateTime.AddDays(1);

            return (startDateTime, endDateTime);
        }

        private static string FormatTime(TimeSpan time)
        {
            return $"{time.Hours:D2}:{time.Minutes:D2}";
        }

        private static string FormatDateTime(object? value)
        {
            if (value == null)
                return string.Empty;

            if (value is DateTime dateTime)
                return dateTime.ToString("dd/MM/yyyy HH:mm");

            if (DateTime.TryParse(value.ToString(), out var parsed))
                return parsed.ToString("dd/MM/yyyy HH:mm");

            return value.ToString() ?? string.Empty;
        }

        private static string? FormatNullableDateTime(DateTime? value)
        {
            return value?.ToString("dd/MM/yyyy HH:mm");
        }

        private static string? FormatDate(object? value)
        {
            if (value == null)
                return null;

            if (value is DateOnly dateOnly)
                return dateOnly.ToString("dd/MM/yyyy");

            if (value is DateTime dateTime)
                return dateTime.ToString("dd/MM/yyyy");

            if (DateTime.TryParse(value.ToString(), out var parsed))
                return parsed.ToString("dd/MM/yyyy");

            return value.ToString();
        }
    }
}
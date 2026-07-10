using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Services
{
    public class ShiftClosingService
    {
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

            var selectedSchedule =
                todaySchedules.FirstOrDefault(s =>
                {
                    var start = ToTimeSpan(s.Shift!.StartTime);
                    var end = ToTimeSpan(s.Shift.EndTime);
                    return IsNowInShiftOrAfter(now, start, end);
                })
                ?? todaySchedules.First();

            var alreadyReported = await _context.KhoShiftClosingReports
                .AnyAsync(r => r.ScheduleId == selectedSchedule.Id);

            return new ClosingShiftInfoDto
            {
                ScheduleId = selectedSchedule.Id,
                ShiftId = selectedSchedule.ShiftId,
                ShiftName = selectedSchedule.Shift?.ShiftName ?? $"Ca #{selectedSchedule.ShiftId}",
                WorkDate = today.ToString("yyyy-MM-dd"),
                StartTime = FormatTime(ToTimeSpan(selectedSchedule.Shift?.StartTime)),
                EndTime = FormatTime(ToTimeSpan(selectedSchedule.Shift?.EndTime)),
                AlreadyReported = alreadyReported
            };
        }

        public async Task<List<ClosingFrontStockItemDto>> GetFrontStockForClosingAsync(int staffId)
        {
            var staff = await GetValidStaffAsync(staffId);

            return await _context.KhoBranchFrontStocks
                .AsNoTracking()
                .Include(f => f.Product)
                .Where(f => f.BranchId == staff.BranchId)
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

        public async Task<int> SubmitShiftClosingReportAsync(int staffId, SubmitShiftClosingDto dto)
        {
            var staff = await GetValidStaffAsync(staffId);

            if (dto.ScheduleId <= 0)
                throw new InvalidOperationException("Không tìm thấy ca cần báo cáo kết ca.");

            if (dto.Items == null || dto.Items.Count == 0)
                throw new InvalidOperationException("Báo cáo kết ca chưa có sản phẩm nào.");

            var schedule = await _context.CaFinalSchedules
                .Include(s => s.Shift)
                .FirstOrDefaultAsync(s => s.Id == dto.ScheduleId && s.UserId == staffId);

            if (schedule == null)
                throw new InvalidOperationException("Không tìm thấy ca làm chính thức của nhân viên.");

            if (schedule.Shift == null)
                throw new InvalidOperationException("Ca làm không hợp lệ.");

            if (schedule.Shift.BranchId != staff.BranchId)
                throw new InvalidOperationException("Ca làm không thuộc cơ sở của nhân viên.");

            var today = DateOnly.FromDateTime(DateTime.Today);
            if (ToDateOnly(schedule.WorkDate) != today)
                throw new InvalidOperationException("Chỉ được báo cáo kết ca cho ca làm trong ngày hiện tại.");

            var alreadyReported = await _context.KhoShiftClosingReports
                .AnyAsync(r => r.ScheduleId == dto.ScheduleId);

            if (alreadyReported)
                throw new InvalidOperationException("Ca này đã có báo cáo kết ca.");

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

            var productIds = submittedItems.Select(i => i.ProductId).ToList();

            var frontStocks = await _context.KhoBranchFrontStocks
                .Include(f => f.Product)
                .Where(f => f.BranchId == staff.BranchId && productIds.Contains(f.ProductId))
                .ToListAsync();

            if (frontStocks.Count != submittedItems.Count)
                throw new InvalidOperationException("Có sản phẩm không tồn tại trong tồn quầy của cơ sở.");

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var report = new KhoShiftClosingReport
                {
                    BranchId = staff.BranchId!.Value,
                    UserId = staffId,
                    ScheduleId = dto.ScheduleId,
                    ReportDate = DateTime.Now,
                    Note = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim()
                };

                _context.KhoShiftClosingReports.Add(report);
                await _context.SaveChangesAsync();

                foreach (var submittedItem in submittedItems)
                {
                    var frontStock = frontStocks.First(f => f.ProductId == submittedItem.ProductId);
                    var systemCount = Convert.ToInt32(frontStock.Quantity);

                    if (submittedItem.ActualCount > systemCount)
                    {
                        throw new InvalidOperationException(
                            $"Sản phẩm '{frontStock.Product.ProductName}' có số lượng thực tế lớn hơn số lượng hệ thống. " +
                            $"Hệ thống: {systemCount}, thực tế: {submittedItem.ActualCount}."
                        );
                    }

                    var detail = new KhoShiftClosingDetail
                    {
                        ReportId = report.Id,
                        ProductId = submittedItem.ProductId,
                        SystemCount = systemCount,
                        ActualCount = submittedItem.ActualCount
                    };

                    _context.KhoShiftClosingDetails.Add(detail);

                    frontStock.Quantity = submittedItem.ActualCount;
                }

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

        public async Task<List<ShiftClosingReportListDto>> GetMyReportsAsync(int staffId)
        {
            return await _context.KhoShiftClosingReports
                .AsNoTracking()
                .Include(r => r.Branch)
                .Include(r => r.User)
                .Include(r => r.Schedule)
                    .ThenInclude(s => s.Shift)
                .Include(r => r.KhoShiftClosingDetails)
                .Where(r => r.UserId == staffId)
                .OrderByDescending(r => r.Id)
                .Select(r => new ShiftClosingReportListDto
                {
                    Id = r.Id,
                    BranchId = r.BranchId,
                    BranchName = r.Branch.Name,
                    UserId = r.UserId,
                    StaffName = r.User.FullName,
                    ScheduleId = r.ScheduleId,
                    ShiftName = r.Schedule != null && r.Schedule.Shift != null ? r.Schedule.Shift.ShiftName : null,
                    WorkDate = r.Schedule != null ? FormatDate(r.Schedule.WorkDate) : null,
                    ReportDate = FormatDateTime(r.ReportDate),
                    ItemCount = r.KhoShiftClosingDetails.Count,
                    TotalSystemCount = r.KhoShiftClosingDetails.Sum(d => d.SystemCount),
                    TotalActualCount = r.KhoShiftClosingDetails.Sum(d => d.ActualCount),
                    TotalDifference = r.KhoShiftClosingDetails.Sum(d => d.SystemCount - d.ActualCount),
                    Note = r.Note
                })
                .ToListAsync();
        }

        public async Task<ShiftClosingReportDetailDto?> GetMyReportDetailAsync(int staffId, int reportId)
        {
            var report = await _context.KhoShiftClosingReports
                .AsNoTracking()
                .Include(r => r.Branch)
                .Include(r => r.User)
                .Include(r => r.Schedule)
                    .ThenInclude(s => s.Shift)
                .Include(r => r.KhoShiftClosingDetails)
                    .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(r => r.Id == reportId && r.UserId == staffId);

            if (report == null)
                return null;

            return new ShiftClosingReportDetailDto
            {
                Id = report.Id,
                BranchId = report.BranchId,
                BranchName = report.Branch.Name,
                UserId = report.UserId,
                StaffName = report.User.FullName,
                ScheduleId = report.ScheduleId,
                ShiftName = report.Schedule?.Shift?.ShiftName,
                WorkDate = report.Schedule != null ? FormatDate(report.Schedule.WorkDate) : null,
                ReportDate = FormatDateTime(report.ReportDate),
                ItemCount = report.KhoShiftClosingDetails.Count,
                TotalSystemCount = report.KhoShiftClosingDetails.Sum(d => d.SystemCount),
                TotalActualCount = report.KhoShiftClosingDetails.Sum(d => d.ActualCount),
                TotalDifference = report.KhoShiftClosingDetails.Sum(d => d.SystemCount - d.ActualCount),
                Note = report.Note,
                Items = report.KhoShiftClosingDetails.Select(d => new ShiftClosingReportItemDto
                {
                    ProductId = d.ProductId,
                    ProductCode = d.Product.ProductCode,
                    ProductName = d.Product.ProductName,
                    Unit = d.Product.Unit,
                    SystemCount = d.SystemCount,
                    ActualCount = d.ActualCount,
                    Difference = d.SystemCount - d.ActualCount
                }).ToList()
            };
        }

        public async Task<List<ShiftClosingReportListDto>> GetReportsForManagementAsync(int? branchId)
        {
            var query = _context.KhoShiftClosingReports
                .AsNoTracking()
                .Include(r => r.Branch)
                .Include(r => r.User)
                .Include(r => r.Schedule)
                    .ThenInclude(s => s.Shift)
                .Include(r => r.KhoShiftClosingDetails)
                .AsQueryable();

            if (branchId.HasValue && branchId.Value > 0)
            {
                query = query.Where(r => r.BranchId == branchId.Value);
            }

            return await query
                .OrderByDescending(r => r.Id)
                .Select(r => new ShiftClosingReportListDto
                {
                    Id = r.Id,
                    BranchId = r.BranchId,
                    BranchName = r.Branch.Name,
                    UserId = r.UserId,
                    StaffName = r.User.FullName,
                    ScheduleId = r.ScheduleId,
                    ShiftName = r.Schedule != null && r.Schedule.Shift != null ? r.Schedule.Shift.ShiftName : null,
                    WorkDate = r.Schedule != null ? FormatDate(r.Schedule.WorkDate) : null,
                    ReportDate = FormatDateTime(r.ReportDate),
                    ItemCount = r.KhoShiftClosingDetails.Count,
                    TotalSystemCount = r.KhoShiftClosingDetails.Sum(d => d.SystemCount),
                    TotalActualCount = r.KhoShiftClosingDetails.Sum(d => d.ActualCount),
                    TotalDifference = r.KhoShiftClosingDetails.Sum(d => d.SystemCount - d.ActualCount),
                    Note = r.Note
                })
                .ToListAsync();
        }

        public async Task<ShiftClosingReportDetailDto?> GetReportDetailForManagementAsync(int reportId, int? branchId)
        {
            var query = _context.KhoShiftClosingReports
                .AsNoTracking()
                .Include(r => r.Branch)
                .Include(r => r.User)
                .Include(r => r.Schedule)
                    .ThenInclude(s => s.Shift)
                .Include(r => r.KhoShiftClosingDetails)
                    .ThenInclude(d => d.Product)
                .AsQueryable();

            if (branchId.HasValue && branchId.Value > 0)
            {
                query = query.Where(r => r.BranchId == branchId.Value);
            }

            var report = await query.FirstOrDefaultAsync(r => r.Id == reportId);

            if (report == null)
                return null;

            return new ShiftClosingReportDetailDto
            {
                Id = report.Id,
                BranchId = report.BranchId,
                BranchName = report.Branch.Name,
                UserId = report.UserId,
                StaffName = report.User.FullName,
                ScheduleId = report.ScheduleId,
                ShiftName = report.Schedule?.Shift?.ShiftName,
                WorkDate = report.Schedule != null ? FormatDate(report.Schedule.WorkDate) : null,
                ReportDate = FormatDateTime(report.ReportDate),
                ItemCount = report.KhoShiftClosingDetails.Count,
                TotalSystemCount = report.KhoShiftClosingDetails.Sum(d => d.SystemCount),
                TotalActualCount = report.KhoShiftClosingDetails.Sum(d => d.ActualCount),
                TotalDifference = report.KhoShiftClosingDetails.Sum(d => d.SystemCount - d.ActualCount),
                Note = report.Note,
                Items = report.KhoShiftClosingDetails.Select(d => new ShiftClosingReportItemDto
                {
                    ProductId = d.ProductId,
                    ProductCode = d.Product.ProductCode,
                    ProductName = d.Product.ProductName,
                    Unit = d.Product.Unit,
                    SystemCount = d.SystemCount,
                    ActualCount = d.ActualCount,
                    Difference = d.SystemCount - d.ActualCount
                }).ToList()
            };
        }

        private async Task<NsUser> GetValidStaffAsync(int staffId)
        {
            if (staffId <= 0)
                throw new InvalidOperationException("Không tìm thấy thông tin nhân viên.");

            var staff = await _context.NsUsers
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == staffId);

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

        private static DateOnly? ToDateOnly(object? value)
        {
            if (value == null) return null;

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
            if (value == null) return TimeSpan.Zero;

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

        private static bool IsNowInShiftOrAfter(TimeSpan now, TimeSpan start, TimeSpan end)
        {
            if (end >= start)
                return now >= start;

            return now >= start || now <= end;
        }

        private static string FormatTime(TimeSpan time)
        {
            return $"{time.Hours:D2}:{time.Minutes:D2}";
        }

        private static string FormatDateTime(object? value)
        {
            if (value == null) return "";

            if (value is DateTime dateTime)
                return dateTime.ToString("dd/MM/yyyy HH:mm");

            if (DateTime.TryParse(value.ToString(), out var parsed))
                return parsed.ToString("dd/MM/yyyy HH:mm");

            return value.ToString() ?? "";
        }

        private static string? FormatDate(object? value)
        {
            if (value == null) return null;

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
using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Repositories
{
    /// <summary>
    /// Thực hiện các thao tác Database
    /// liên quan đến báo cáo kết ca.
    ///
    /// Repository sẽ chịu trách nhiệm:
    /// - Truy vấn nhân viên và quản lý.
    /// - Truy vấn lịch làm và điểm danh.
    /// - Truy vấn tồn quầy.
    /// - Tạo và cập nhật báo cáo kết ca.
    /// - Lấy lịch sử báo cáo.
    /// - Quản lý transaction.
    /// </summary>
    public class ShiftClosingRepo
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// Nhận AppDbContext thông qua
        /// Dependency Injection.
        /// </summary>
        public ShiftClosingRepo(
            AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Tìm người dùng theo ID
        /// và lấy kèm thông tin vai trò.
        ///
        /// Không lấy tài khoản đã bị xóa mềm.
        /// </summary>
        public async Task<NsUser?> GetUserByIdAsync(
            int userId)
        {
            return await _context.NsUsers
                .Include(user => user.Role)
                .FirstOrDefaultAsync(user =>
                    user.Id == userId &&
                    user.IsDeleted != true
                );
        }

        /// <summary>
        /// Lấy các lịch làm chính thức
        /// của một người dùng.
        /// </summary>
        public async Task<List<CaFinalSchedule>>
            GetSchedulesByUserIdAsync(
                int userId)
        {
            return await _context.CaFinalSchedules
                .AsNoTracking()
                .Include(schedule =>
                    schedule.Shift
                )
                .Where(schedule =>
                    schedule.UserId == userId
                )
                .ToListAsync();
        }

        /// <summary>
        /// Tìm lịch làm theo ScheduleId
        /// và ID nhân viên.
        /// </summary>
        public async Task<CaFinalSchedule?>
            GetScheduleByIdAndUserIdAsync(
                int scheduleId,
                int userId)
        {
            return await _context.CaFinalSchedules
                .Include(schedule =>
                    schedule.Shift
                )
                .FirstOrDefaultAsync(schedule =>
                    schedule.Id == scheduleId &&
                    schedule.UserId == userId
                );
        }

        /// <summary>
        /// Lấy thông tin điểm danh
        /// theo lịch làm chính thức.
        /// </summary>
        public async Task<CaAttendance?>
            GetAttendanceByScheduleIdAsync(
                int scheduleId)
        {
            return await _context.CaAttendances
                .AsNoTracking()
                .FirstOrDefaultAsync(attendance =>
                    attendance.ScheduleId ==
                    scheduleId
                );
        }

        /// <summary>
        /// Lấy danh sách tồn quầy của một chi nhánh.
        ///
        /// Không lọc theo trạng thái kinh doanh của sản phẩm.
        /// Sản phẩm đã ngừng vẫn phải xuất hiện nếu còn tồn quầy
        /// để nhân viên kiểm kê và báo cáo kết ca.
        /// </summary>
        public async Task<List<KhoBranchFrontStock>>
            GetFrontStocksByBranchAsync(
                int branchId)
        {
            return await _context
                .KhoBranchFrontStocks
                .AsNoTracking()
                .Include(frontStock =>
                    frontStock.Product
                )
                .Where(frontStock =>
                    frontStock.BranchId ==
                        branchId
                )
                .OrderBy(frontStock =>
                    frontStock.Product.ProductName
                )
                .ToListAsync();
        }

        // =====================================================
// LỊCH SỬ BÁO CÁO KẾT CA
// =====================================================

/// <summary>
/// Lấy danh sách báo cáo kết ca
/// do một Staff đã gửi.
/// </summary>
public async Task<List<ShiftClosingReportListDto>>
    GetReportsByStaffIdAsync(
        int staffId)
{
    var reports =
        await _context.KhoShiftClosingReports
            .AsNoTracking()
            .AsSplitQuery()
            .Include(report =>
                report.Branch
            )
            .Include(report =>
                report.User
            )
            .Include(report =>
                report.ReviewedByNavigation
            )
            .Include(report =>
                report.Schedule
            )
                .ThenInclude(schedule =>
                    schedule!.Shift
                )
            .Include(report =>
                report.KhoShiftClosingDetails
            )
            .Where(report =>
                report.UserId == staffId
            )
            .OrderByDescending(report =>
                report.Id
            )
            .ToListAsync();

    return reports
        .Select(MapReportList)
        .ToList();
}

/// <summary>
/// Lấy chi tiết một báo cáo kết ca
/// thuộc về Staff đang đăng nhập.
///
/// Điều kiện UserId ngăn Staff xem
/// báo cáo của người khác.
/// </summary>
public async Task<ShiftClosingReportDetailDto?>
    GetReportDetailByStaffAsync(
        int staffId,
        int reportId)
{
    var report =
        await _context.KhoShiftClosingReports
            .AsNoTracking()
            .AsSplitQuery()
            .Include(item =>
                item.Branch
            )
            .Include(item =>
                item.User
            )
            .Include(item =>
                item.ReviewedByNavigation
            )
            .Include(item =>
                item.Schedule
            )
                .ThenInclude(schedule =>
                    schedule!.Shift
                )
            .Include(item =>
                item.KhoShiftClosingDetails
            )
                .ThenInclude(detail =>
                    detail.Product
                )
            .FirstOrDefaultAsync(item =>
                item.Id == reportId &&
                item.UserId == staffId
            );

    return report == null
        ? null
        : MapReportDetail(report);
}

/// <summary>
/// Lấy danh sách báo cáo kết ca
/// dành cho Manager hoặc Admin.
///
/// branchId null:
/// lấy báo cáo toàn hệ thống.
///
/// branchId có giá trị:
/// chỉ lấy báo cáo của chi nhánh đó.
/// </summary>
public async Task<List<ShiftClosingReportListDto>>
    GetReportsForManagementAsync(
        int? branchId)
{
    var query =
        _context.KhoShiftClosingReports
            .AsNoTracking()
            .AsSplitQuery()
            .Include(report =>
                report.Branch
            )
            .Include(report =>
                report.User
            )
            .Include(report =>
                report.ReviewedByNavigation
            )
            .Include(report =>
                report.Schedule
            )
                .ThenInclude(schedule =>
                    schedule!.Shift
                )
            .Include(report =>
                report.KhoShiftClosingDetails
            )
            .AsQueryable();

    if (
        branchId.HasValue &&
        branchId.Value > 0
    )
    {
        query = query.Where(report =>
            report.BranchId ==
            branchId.Value
        );
    }

    var reports =
        await query
            .OrderByDescending(report =>
                report.Id
            )
            .ToListAsync();

    return reports
        .Select(MapReportList)
        .ToList();
}

/// <summary>
/// Lấy chi tiết báo cáo kết ca
/// dành cho Manager hoặc Admin.
///
/// Khi branchId có giá trị,
/// báo cáo phải thuộc đúng chi nhánh đó.
/// </summary>
public async Task<ShiftClosingReportDetailDto?>
    GetReportDetailForManagementAsync(
        int reportId,
        int? branchId)
{
    var query =
        _context.KhoShiftClosingReports
            .AsNoTracking()
            .AsSplitQuery()
            .Include(report =>
                report.Branch
            )
            .Include(report =>
                report.User
            )
            .Include(report =>
                report.ReviewedByNavigation
            )
            .Include(report =>
                report.Schedule
            )
                .ThenInclude(schedule =>
                    schedule!.Shift
                )
            .Include(report =>
                report.KhoShiftClosingDetails
            )
                .ThenInclude(detail =>
                    detail.Product
                )
            .AsQueryable();

    if (
        branchId.HasValue &&
        branchId.Value > 0
    )
    {
        query = query.Where(report =>
            report.BranchId ==
            branchId.Value
        );
    }

    var result =
        await query.FirstOrDefaultAsync(
            report =>
                report.Id == reportId
        );

    return result == null
        ? null
        : MapReportDetail(result);
}

// =====================================================
// CHUYỂN ENTITY THÀNH DTO
// =====================================================
// =====================================================
// TRẠNG THÁI VÀ BÁO CÁO ĐANG HOẠT ĐỘNG
// =====================================================

/// <summary>
/// Lấy báo cáo của một lịch làm cụ thể.
///
/// Phương thức dùng AsNoTracking vì chỉ phục vụ
/// kiểm tra trạng thái báo cáo.
/// </summary>
public async Task<KhoShiftClosingReport?>
    GetReportByScheduleIdAsNoTrackingAsync(
        int scheduleId)
{
    return await _context
        .KhoShiftClosingReports
        .AsNoTracking()
        .FirstOrDefaultAsync(report =>
            report.ScheduleId == scheduleId
        );
}

/// <summary>
/// Tìm báo cáo đang hoạt động của cùng
/// chi nhánh, ngày làm và ca làm.
///
/// Báo cáo PENDING hoặc APPROVED sẽ khóa
/// quyền gửi báo cáo của các Staff khác.
/// </summary>
public async Task<KhoShiftClosingReport?>
    GetActiveShiftReportAsync(
        int branchId,
        int shiftId,
        DateOnly workDate,
        string pendingStatus,
        string approvedStatus)
{
    return await _context
        .KhoShiftClosingReports
        .AsNoTracking()
        .Where(report =>
            report.BranchId == branchId &&
            report.Schedule != null &&
            report.Schedule.ShiftId == shiftId &&
            report.Schedule.WorkDate == workDate &&
            (
                report.Status == pendingStatus ||
                report.Status == approvedStatus
            )
        )
        .OrderBy(report =>
            report.Status == pendingStatus
                ? 0
                : 1
        )
        .ThenByDescending(report =>
            report.Id
        )
        .FirstOrDefaultAsync();
}

// =====================================================
// TỒN QUẦY PHỤC VỤ GỬI VÀ DUYỆT BÁO CÁO
// =====================================================

/// <summary>
/// Lấy các sản phẩm tồn quầy đang hoạt động
/// theo danh sách ProductId.
///
/// Dùng khi Staff gửi báo cáo kết ca.
/// Dữ liệu chỉ được đọc nên dùng AsNoTracking.
/// </summary>
public async Task<List<KhoBranchFrontStock>>
    GetFrontStocksByProductIdsAsync(
        int branchId,
        List<int> productIds)
{
    return await _context
        .KhoBranchFrontStocks
        .AsNoTracking()
        .Include(frontStock =>
            frontStock.Product
        )
        .Where(frontStock =>
            frontStock.BranchId == branchId &&
            productIds.Contains(
                frontStock.ProductId
            )
        )
        .ToListAsync();
}

/// <summary>
/// Lấy các dòng tồn quầy có tracking
/// để cập nhật số lượng khi Manager duyệt.
///
/// Không dùng AsNoTracking vì Service cần thay đổi
/// thuộc tính Quantity rồi gọi SaveChanges.
/// </summary>
public async Task<List<KhoBranchFrontStock>>
    GetTrackedFrontStocksByProductIdsAsync(
        int branchId,
        List<int> productIds)
{
    return await _context
        .KhoBranchFrontStocks
        .Where(frontStock =>
            frontStock.BranchId == branchId &&
            productIds.Contains(
                frontStock.ProductId
            )
        )
        .ToListAsync();
}

// =====================================================
// KHÓA CA VÀ KIỂM SOÁT GỬI TRÙNG
// =====================================================

/// <summary>
/// Khóa bản ghi ca làm bằng SELECT FOR UPDATE.
///
/// Khóa này bảo đảm hai Staff trong cùng ca
/// không thể đồng thời tạo hai báo cáo PENDING.
/// Phương thức phải được gọi bên trong transaction.
/// </summary>
public async Task LockShiftForUpdateAsync(
    int shiftId)
{
    await _context.CaShifts
        .FromSqlInterpolated(
            $"SELECT * FROM ca_shift WHERE id = {shiftId} FOR UPDATE"
        )
        .AsNoTracking()
        .SingleAsync();
}

// =====================================================
// TẠO VÀ GỬI LẠI BÁO CÁO
// =====================================================

/// <summary>
/// Lấy báo cáo theo ScheduleId
/// kèm các chi tiết kiểm kê.
///
/// Có tracking để phục vụ trường hợp cập nhật
/// và gửi lại báo cáo đã bị từ chối.
/// </summary>
public async Task<KhoShiftClosingReport?>
    GetReportByScheduleIdWithDetailsAsync(
        int scheduleId)
{
    return await _context
        .KhoShiftClosingReports
        .Include(report =>
            report.KhoShiftClosingDetails
        )
        .FirstOrDefaultAsync(report =>
            report.ScheduleId == scheduleId
        );
}

/// <summary>
/// Thêm báo cáo mới và lưu ngay
/// để lấy được ReportId.
/// </summary>
public async Task<KhoShiftClosingReport>
    AddReportAsync(
        KhoShiftClosingReport report)
{
    _context.KhoShiftClosingReports.Add(
        report
    );

    await _context.SaveChangesAsync();

    return report;
}

/// <summary>
/// Xóa các chi tiết cũ của báo cáo
/// trước khi Staff gửi lại.
///
/// Phương thức này chưa gọi SaveChanges.
/// </summary>
public void RemoveReportDetails(
    IEnumerable<KhoShiftClosingDetail> details)
{
    _context.KhoShiftClosingDetails
        .RemoveRange(details);
}

/// <summary>
/// Thêm danh sách chi tiết kiểm kê
/// vào một báo cáo kết ca.
///
/// Phương thức này chưa gọi SaveChanges.
/// </summary>
public void AddReportDetails(
    IEnumerable<KhoShiftClosingDetail> details)
{
    _context.KhoShiftClosingDetails
        .AddRange(details);
}

// =====================================================
// DUYỆT VÀ TỪ CHỐI BÁO CÁO
// =====================================================

/// <summary>
/// Lấy báo cáo cần duyệt,
/// kèm chi tiết và thông tin sản phẩm.
///
/// Dữ liệu có tracking để cập nhật trạng thái
/// và thông tin người duyệt.
/// </summary>
public async Task<KhoShiftClosingReport?>
    GetReportForApprovalAsync(
        int reportId)
{
    return await _context
        .KhoShiftClosingReports
        .Include(report =>
            report.KhoShiftClosingDetails
        )
            .ThenInclude(detail =>
                detail.Product
            )
        .FirstOrDefaultAsync(report =>
            report.Id == reportId
        );
}

/// <summary>
/// Lấy báo cáo theo ID có tracking.
///
/// Phương thức dùng cho nghiệp vụ từ chối,
/// vì không cần tải toàn bộ chi tiết kiểm kê.
/// </summary>
public async Task<KhoShiftClosingReport?>
    GetReportByIdAsync(
        int reportId)
{
    return await _context
        .KhoShiftClosingReports
        .FirstOrDefaultAsync(report =>
            report.Id == reportId
        );
}

// =====================================================
// LƯU DỮ LIỆU VÀ TRANSACTION
// =====================================================

/// <summary>
/// Lưu toàn bộ thay đổi đang được
/// AppDbContext theo dõi.
/// </summary>
public async Task SaveChangesAsync()
{
    await _context.SaveChangesAsync();
}

/// <summary>
/// Thực hiện một nghiệp vụ có kết quả trả về
/// trong cùng một transaction.
///
/// Thành công thì commit.
/// Có lỗi thì rollback.
/// </summary>
public async Task<T>
    ExecuteInTransactionAsync<T>(
        Func<Task<T>> action)
{
    await using var transaction =
        await _context.Database
            .BeginTransactionAsync();

    try
    {
        var result = await action();

        await transaction.CommitAsync();

        return result;
    }
    catch
    {
        await transaction.RollbackAsync();

        throw;
    }
}

/// <summary>
/// Thực hiện một nghiệp vụ không có
/// kết quả trả về trong cùng transaction.
///
/// Thành công thì commit.
/// Có lỗi thì rollback.
/// </summary>
public async Task ExecuteInTransactionAsync(
    Func<Task> action)
{
    await using var transaction =
        await _context.Database
            .BeginTransactionAsync();

    try
    {
        await action();

        await transaction.CommitAsync();
    }
    catch
    {
        await transaction.RollbackAsync();

        throw;
    }
}
/// <summary>
/// Chuyển báo cáo kết ca thành DTO
/// dùng trong màn hình danh sách.
/// </summary>
private static ShiftClosingReportListDto
    MapReportList(
        KhoShiftClosingReport report)
{
    return new ShiftClosingReportListDto
    {
        Id = report.Id,

        BranchId =
            report.BranchId,

        BranchName =
            report.Branch?.Name ??
            "Chưa rõ cơ sở",

        UserId =
            report.UserId,

        StaffName =
            report.User?.FullName ??
            string.Empty,

        ScheduleId =
            report.ScheduleId,

        ShiftName =
            report.Schedule
                ?.Shift
                ?.ShiftName,

        WorkDate =
            report.Schedule == null
                ? null
                : FormatDate(
                    report.Schedule.WorkDate
                ),

        ReportDate =
            FormatDateTime(
                report.ReportDate
            ),

        ItemCount =
            report.KhoShiftClosingDetails
                .Count,

        TotalSystemCount =
            report.KhoShiftClosingDetails
                .Sum(detail =>
                    detail.SystemCount
                ),

        TotalActualCount =
            report.KhoShiftClosingDetails
                .Sum(detail =>
                    detail.ActualCount
                ),

        TotalDifference =
            report.KhoShiftClosingDetails
                .Sum(detail =>
                    detail.SystemCount -
                    detail.ActualCount
                ),

        Note =
            report.Note,

        Status =
            report.Status,

        ReviewedBy =
            report.ReviewedBy,

        ReviewerName =
            report.ReviewedByNavigation
                ?.FullName,

        ReviewedAt =
            FormatNullableDateTime(
                report.ReviewedAt
            ),

        RejectReason =
            report.RejectReason
    };
}

/// <summary>
/// Chuyển báo cáo kết ca thành DTO
/// dùng trong màn hình chi tiết.
/// </summary>
private static ShiftClosingReportDetailDto
    MapReportDetail(
        KhoShiftClosingReport report)
{
    return new ShiftClosingReportDetailDto
    {
        Id = report.Id,

        BranchId =
            report.BranchId,

        BranchName =
            report.Branch?.Name ??
            "Chưa rõ cơ sở",

        UserId =
            report.UserId,

        StaffName =
            report.User?.FullName ??
            string.Empty,

        ScheduleId =
            report.ScheduleId,

        ShiftName =
            report.Schedule
                ?.Shift
                ?.ShiftName,

        WorkDate =
            report.Schedule == null
                ? null
                : FormatDate(
                    report.Schedule.WorkDate
                ),

        ReportDate =
            FormatDateTime(
                report.ReportDate
            ),

        ItemCount =
            report.KhoShiftClosingDetails
                .Count,

        TotalSystemCount =
            report.KhoShiftClosingDetails
                .Sum(detail =>
                    detail.SystemCount
                ),

        TotalActualCount =
            report.KhoShiftClosingDetails
                .Sum(detail =>
                    detail.ActualCount
                ),

        TotalDifference =
            report.KhoShiftClosingDetails
                .Sum(detail =>
                    detail.SystemCount -
                    detail.ActualCount
                ),

        Note =
            report.Note,

        Status =
            report.Status,

        ReviewedBy =
            report.ReviewedBy,

        ReviewerName =
            report.ReviewedByNavigation
                ?.FullName,

        ReviewedAt =
            FormatNullableDateTime(
                report.ReviewedAt
            ),

        RejectReason =
            report.RejectReason,

        Items =
            report.KhoShiftClosingDetails
                .Select(detail =>
                    new ShiftClosingReportItemDto
                    {
                        ProductId =
                            detail.ProductId,

                        ProductCode =
                            detail.Product
                                ?.ProductCode,

                        ProductName =
                            detail.Product
                                ?.ProductName ??
                            "Chưa rõ sản phẩm",

                        Unit =
                            detail.Product?.Unit,

                        SystemCount =
                            detail.SystemCount,

                        ActualCount =
                            detail.ActualCount,

                        Difference =
                            detail.SystemCount -
                            detail.ActualCount
                    }
                )
                .ToList()
    };
}

// =====================================================
// HÀM ĐỊNH DẠNG
// =====================================================

/// <summary>
/// Định dạng ngày giờ báo cáo
/// theo kiểu Việt Nam.
/// </summary>
private static string FormatDateTime(
    object? value)
{
    if (value == null)
    {
        return string.Empty;
    }

    if (value is DateTime dateTime)
    {
        return dateTime.ToString(
            "dd/MM/yyyy HH:mm"
        );
    }

    if (
        DateTime.TryParse(
            value.ToString(),
            out var parsedDateTime
        )
    )
    {
        return parsedDateTime.ToString(
            "dd/MM/yyyy HH:mm"
        );
    }

    return value.ToString() ??
        string.Empty;
}

/// <summary>
/// Định dạng ngày giờ duyệt báo cáo.
/// </summary>
private static string?
    FormatNullableDateTime(
        DateTime? value)
{
    return value?.ToString(
        "dd/MM/yyyy HH:mm"
    );
}

/// <summary>
/// Định dạng ngày làm việc.
/// </summary>
private static string? FormatDate(
    object? value)
{
    if (value == null)
    {
        return null;
    }

    if (value is DateOnly dateOnly)
    {
        return dateOnly.ToString(
            "dd/MM/yyyy"
        );
    }

    if (value is DateTime dateTime)
    {
        return dateTime.ToString(
            "dd/MM/yyyy"
        );
    }

    if (
        DateTime.TryParse(
            value.ToString(),
            out var parsedDate
        )
    )
    {
        return parsedDate.ToString(
            "dd/MM/yyyy"
        );
    }

    return value.ToString();
}
    }

    
}
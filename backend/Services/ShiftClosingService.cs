using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Repositories;

namespace LuanVanTotNghiep.Services
{
    public class ShiftClosingService
    {
        private const string StatusPending = "PENDING";
        private const string StatusApproved = "APPROVED";
        private const string StatusRejected = "REJECTED";



        private readonly ShiftClosingRepo _shiftClosingRepo;

/// <summary>
/// Nhận ShiftClosingRepo thông qua
/// Dependency Injection.
/// </summary>
public ShiftClosingService(
    ShiftClosingRepo shiftClosingRepo)
{
    _shiftClosingRepo = shiftClosingRepo;
}

        /// <summary>
/// Lấy ca làm trong ngày mà Staff
/// cần thực hiện báo cáo kết ca.
/// </summary>
public async Task<ClosingShiftInfoDto?>
    GetTodayClosingShiftAsync(
        int staffId)
{
    var staff =
        await GetValidStaffAsync(
            staffId
        );

    var branchId =
        staff.BranchId!.Value;

    var today =
        DateOnly.FromDateTime(
            DateTime.Today
        );

    var now =
        DateTime.Now.TimeOfDay;

    // Lấy lịch làm chính thức
    // thông qua Repository.
    var schedules =
        await _shiftClosingRepo
            .GetSchedulesByUserIdAsync(
                staffId
            );

    var todaySchedules =
        schedules
            .Where(schedule =>
                ToDateOnly(
                    schedule.WorkDate
                ) == today
            )
            .Where(schedule =>
                schedule.Shift != null
            )
            .Where(schedule =>
                schedule.Shift!.BranchId ==
                branchId
            )
            .OrderBy(schedule =>
                ToTimeSpan(
                    schedule.Shift!.StartTime
                )
            )
            .ToList();

    if (todaySchedules.Count == 0)
    {
        return null;
    }

    // Ưu tiên ca đã bắt đầu gần nhất.
    // Nếu chưa có ca nào bắt đầu,
    // lấy ca đầu tiên trong ngày.
    var selectedSchedule =
        todaySchedules
            .Where(schedule =>
                now >= ToTimeSpan(
                    schedule.Shift!.StartTime
                )
            )
            .OrderByDescending(schedule =>
                ToTimeSpan(
                    schedule.Shift!.StartTime
                )
            )
            .FirstOrDefault()
        ?? todaySchedules.First();

    var selectedShift =
        selectedSchedule.Shift!;

    var selectedWorkDate =
        ToDateOnly(
            selectedSchedule.WorkDate
        ) ?? today;

    // Lấy thông tin điểm danh.
    var attendance =
        await _shiftClosingRepo
            .GetAttendanceByScheduleIdAsync(
                selectedSchedule.Id
            );

    // Lấy báo cáo thuộc lịch của
    // chính Staff hiện tại.
    var ownReport =
        await _shiftClosingRepo
            .GetReportByScheduleIdAsNoTrackingAsync(
                selectedSchedule.Id
            );

    // Tìm báo cáo PENDING hoặc APPROVED
    // đang khóa quyền gửi của ca.
    var activeShiftReport =
        await _shiftClosingRepo
            .GetActiveShiftReportAsync(
                branchId,
                selectedSchedule.ShiftId,
                selectedWorkDate,
                StatusPending,
                StatusApproved
            );

    var activeReportIsOwn =
        activeShiftReport?.ScheduleId ==
        selectedSchedule.Id;

    string reportStatus;
    int? reportId;
    string? rejectReason;

    if (activeShiftReport != null)
    {
        reportStatus =
            NormalizeStatus(
                activeShiftReport.Status,
                StatusPending
            );

        // Không cung cấp ReportId của
        // nhân viên khác cho Staff hiện tại.
        reportId =
            activeReportIsOwn
                ? activeShiftReport.Id
                : null;

        rejectReason = null;
    }
    else
    {
        var ownStatus =
            NormalizeStatus(
                ownReport?.Status,
                "NONE"
            );

        if (ownStatus == StatusRejected)
        {
            reportStatus =
                StatusRejected;

            reportId =
                ownReport?.Id;

            rejectReason =
                ownReport?.RejectReason;
        }
        else
        {
            reportStatus = "NONE";
            reportId = null;
            rejectReason = null;
        }
    }

    var alreadyReported =
        activeShiftReport != null;

    var hasCheckedIn =
        attendance?.CheckInTime != null;

    var hasCheckedOut =
        attendance?.CheckOutTime != null;

    var startTime =
        ToTimeSpan(
            selectedShift.StartTime
        );

    var endTime =
        ToTimeSpan(
            selectedShift.EndTime
        );

    var (_, shiftEndDateTime) =
        GetShiftDateTimeRange(
            selectedWorkDate,
            startTime,
            endTime
        );

    var isShiftEnded =
        DateTime.Now >
        shiftEndDateTime;

    var isPublished =
        string.Equals(
            selectedSchedule.Status,
            "PUBLISHED",
            StringComparison.OrdinalIgnoreCase
        );

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
    else if (
        activeShiftReport != null &&
        reportStatus == StatusPending
    )
    {
        submitBlockReason =
            activeReportIsOwn
                ? "Báo cáo của bạn đang chờ Quản lý duyệt."
                : "Ca này đã có nhân viên khác gửi báo cáo và đang chờ Quản lý duyệt.";
    }
    else if (
        activeShiftReport != null &&
        reportStatus == StatusApproved
    )
    {
        submitBlockReason =
            activeReportIsOwn
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
        ScheduleId =
            selectedSchedule.Id,

        ShiftId =
            selectedSchedule.ShiftId,

        ShiftName =
            selectedShift.ShiftName ??
            $"Ca #{selectedSchedule.ShiftId}",

        WorkDate =
            selectedWorkDate.ToString(
                "yyyy-MM-dd"
            ),

        StartTime =
            FormatTime(startTime),

        EndTime =
            FormatTime(endTime),

        ReportId =
            reportId,

        ReportStatus =
            reportStatus,

        RejectReason =
            rejectReason,

        AlreadyReported =
            alreadyReported,

        HasCheckedIn =
            hasCheckedIn,

        HasCheckedOut =
            hasCheckedOut,

        IsShiftEnded =
            isShiftEnded,

        CanSubmit =
            canSubmit,

        SubmitBlockReason =
            submitBlockReason
    };
}

        /// <summary>
        /// Lấy danh sách tồn quầy của cơ sở
        /// để nhân viên thực hiện kiểm kê kết ca.
        /// </summary>
        public async Task<List<ClosingFrontStockItemDto>>
            GetFrontStockForClosingAsync(
                int staffId)
        {
            // Kiểm tra tài khoản Staff
            // và xác định cơ sở đang làm việc.
            var staff =
                await GetValidStaffAsync(
                    staffId
                );

            // GetValidStaffAsync đã kiểm tra BranchId,
            // nên tại đây BranchId chắc chắn hợp lệ.
            var branchId =
                staff.BranchId!.Value;

            // Lấy tồn quầy đang hoạt động
            // thông qua Repository.
            var frontStocks =
                await _shiftClosingRepo
                    .GetActiveFrontStocksByBranchAsync(
                        branchId
                    );

            // Chuyển Entity sang DTO để trả về Frontend.
            return frontStocks
                .Select(frontStock =>
                    new ClosingFrontStockItemDto
                    {
                        ProductId =
                            frontStock.ProductId,

                        ProductCode =
                            frontStock.Product
                                .ProductCode,

                        ProductName =
                            frontStock.Product
                                .ProductName,

                        Unit =
                            frontStock.Product.Unit,

                        SystemCount =
                            frontStock.Quantity ?? 0,

                        ActualCount =
                            frontStock.Quantity ?? 0
                    }
                )
                .ToList();
        }

       /// <summary>
/// Gửi báo cáo kiểm kê kết ca.
///
/// Chỉ Staff đã check-in, chưa checkout
/// và có lịch chính thức trong ngày mới được gửi.
/// </summary>
public async Task<int>
    SubmitShiftClosingReportAsync(
        int staffId,
        SubmitShiftClosingDto dto)
{
    var staff =
        await GetValidStaffAsync(
            staffId
        );

    var branchId =
        staff.BranchId!.Value;

    if (dto.ScheduleId <= 0)
    {
        throw new InvalidOperationException(
            "Không tìm thấy ca cần báo cáo kết ca."
        );
    }

    if (
        dto.Items == null ||
        dto.Items.Count == 0
    )
    {
        throw new InvalidOperationException(
            "Báo cáo kết ca chưa có sản phẩm nào."
        );
    }

    var note =
        string.IsNullOrWhiteSpace(
            dto.Note
        )
            ? null
            : dto.Note.Trim();

    if (note?.Length > 255)
    {
        throw new InvalidOperationException(
            "Ghi chú không được vượt quá 255 ký tự."
        );
    }

    // Lấy lịch làm đúng Staff
    // thông qua Repository.
    var schedule =
        await _shiftClosingRepo
            .GetScheduleByIdAndUserIdAsync(
                dto.ScheduleId,
                staffId
            );

    if (schedule == null)
    {
        throw new InvalidOperationException(
            "Không tìm thấy ca làm chính thức của nhân viên."
        );
    }

    if (schedule.Shift == null)
    {
        throw new InvalidOperationException(
            "Ca làm không hợp lệ."
        );
    }

    if (
        schedule.Shift.BranchId !=
        branchId
    )
    {
        throw new InvalidOperationException(
            "Ca làm không thuộc cơ sở của nhân viên."
        );
    }

    var scheduleDate =
        ToDateOnly(
            schedule.WorkDate
        );

    var today =
        DateOnly.FromDateTime(
            DateTime.Today
        );

    if (
        !scheduleDate.HasValue ||
        scheduleDate.Value != today
    )
    {
        throw new InvalidOperationException(
            "Chỉ được báo cáo kết ca cho ca làm trong ngày hiện tại."
        );
    }

    if (
        !string.Equals(
            schedule.Status,
            "PUBLISHED",
            StringComparison.OrdinalIgnoreCase
        )
    )
    {
        throw new InvalidOperationException(
            "Ca làm chưa được công bố chính thức."
        );
    }

    // Lấy dữ liệu điểm danh.
    var attendance =
        await _shiftClosingRepo
            .GetAttendanceByScheduleIdAsync(
                schedule.Id
            );

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

    // Gộp các dòng trùng ProductId.
    // Dòng cuối cùng quyết định ActualCount.
    var submittedItems =
        dto.Items
            .Where(item =>
                item.ProductId > 0
            )
            .GroupBy(item =>
                item.ProductId
            )
            .Select(group =>
                new SubmitShiftClosingItemDto
                {
                    ProductId =
                        group.Key,

                    ActualCount =
                        group.Last().ActualCount
                }
            )
            .ToList();

    if (submittedItems.Count == 0)
    {
        throw new InvalidOperationException(
            "Danh sách kiểm kê không hợp lệ."
        );
    }

    foreach (var item in submittedItems)
    {
        if (item.ActualCount < 0)
        {
            throw new InvalidOperationException(
                "Số lượng thực tế không được âm."
            );
        }
    }

    var productIds =
        submittedItems
            .Select(item =>
                item.ProductId
            )
            .Distinct()
            .ToList();

    // Lấy tồn quầy của các sản phẩm
    // được Staff gửi lên.
    var frontStocks =
        await _shiftClosingRepo
            .GetActiveFrontStocksByProductIdsAsync(
                branchId,
                productIds
            );

    if (
        frontStocks.Count !=
        productIds.Count
    )
    {
        throw new InvalidOperationException(
            "Có sản phẩm không tồn tại trong tồn quầy hoặc đã ngừng kinh doanh."
        );
    }

    foreach (
        var submittedItem in submittedItems
    )
    {
        var frontStock =
            frontStocks.First(item =>
                item.ProductId ==
                submittedItem.ProductId
            );

        var systemCount =
            frontStock.Quantity ?? 0;

        if (
            submittedItem.ActualCount >
            systemCount
        )
        {
            throw new InvalidOperationException(
                $"Sản phẩm '{frontStock.Product.ProductName}' có số lượng thực tế lớn hơn số lượng hệ thống. " +
                $"Hệ thống: {systemCount}, thực tế: {submittedItem.ActualCount}."
            );
        }
    }

    // Transaction bảo đảm toàn bộ báo cáo
    // và chi tiết được lưu đồng nhất.
    return await _shiftClosingRepo
        .ExecuteInTransactionAsync(
            async () =>
            {
                // Khóa bản ghi ca để ngăn hai Staff
                // cùng lúc tạo báo cáo PENDING.
                await _shiftClosingRepo
                    .LockShiftForUpdateAsync(
                        schedule.ShiftId
                    );

                // Kiểm tra lại sau khi đã khóa ca.
                var activeShiftReport =
                    await _shiftClosingRepo
                        .GetActiveShiftReportAsync(
                            branchId,
                            schedule.ShiftId,
                            scheduleDate.Value,
                            StatusPending,
                            StatusApproved
                        );

                if (activeShiftReport != null)
                {
                    var activeStatus =
                        NormalizeStatus(
                            activeShiftReport.Status,
                            StatusPending
                        );

                    var isOwnActiveReport =
                        activeShiftReport.ScheduleId ==
                        dto.ScheduleId;

                    if (
                        activeStatus ==
                        StatusPending
                    )
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

                // Kiểm tra báo cáo cũ của chính lịch làm.
                var existingReport =
                    await _shiftClosingRepo
                        .GetReportByScheduleIdWithDetailsAsync(
                            dto.ScheduleId
                        );

                KhoShiftClosingReport report;

                if (existingReport == null)
                {
                    report =
                        new KhoShiftClosingReport
                        {
                            BranchId =
                                branchId,

                            UserId =
                                staffId,

                            ScheduleId =
                                dto.ScheduleId,

                            ReportDate =
                                DateTime.Now,

                            Note =
                                note,

                            Status =
                                StatusPending,

                            ReviewedBy =
                                null,

                            ReviewedAt =
                                null,

                            RejectReason =
                                null
                        };

                    // Lưu trước để lấy ReportId.
                    await _shiftClosingRepo
                        .AddReportAsync(
                            report
                        );
                }
                else
                {
                    var currentStatus =
                        NormalizeStatus(
                            existingReport.Status,
                            StatusPending
                        );

                    if (
                        currentStatus ==
                        StatusPending
                    )
                    {
                        throw new InvalidOperationException(
                            "Báo cáo của ca này đang chờ Quản lý duyệt."
                        );
                    }

                    if (
                        currentStatus ==
                        StatusApproved
                    )
                    {
                        throw new InvalidOperationException(
                            "Báo cáo của ca này đã được Quản lý duyệt."
                        );
                    }

                    if (
                        currentStatus !=
                        StatusRejected
                    )
                    {
                        throw new InvalidOperationException(
                            "Trạng thái báo cáo hiện tại không hợp lệ."
                        );
                    }

                    // Báo cáo bị từ chối được sử dụng lại
                    // do ScheduleId là duy nhất.
                    _shiftClosingRepo
                        .RemoveReportDetails(
                            existingReport
                                .KhoShiftClosingDetails
                        );

                    existingReport.ReportDate =
                        DateTime.Now;

                    existingReport.Note =
                        note;

                    existingReport.Status =
                        StatusPending;

                    existingReport.ReviewedBy =
                        null;

                    existingReport.ReviewedAt =
                        null;

                    existingReport.RejectReason =
                        null;

                    report =
                        existingReport;

                    // Xóa chi tiết cũ trước để không
                    // vi phạm khóa duy nhất.
                    await _shiftClosingRepo
                        .SaveChangesAsync();
                }

                var details =
                    submittedItems
                        .Select(submittedItem =>
                        {
                            var frontStock =
                                frontStocks.First(item =>
                                    item.ProductId ==
                                    submittedItem.ProductId
                                );

                            return new KhoShiftClosingDetail
                            {
                                ReportId =
                                    report.Id,

                                ProductId =
                                    submittedItem.ProductId,

                                SystemCount =
                                    frontStock.Quantity ?? 0,

                                ActualCount =
                                    submittedItem.ActualCount
                            };
                        })
                        .ToList();

                _shiftClosingRepo
                    .AddReportDetails(
                        details
                    );

                // Không cập nhật tồn quầy tại đây.
                // Chỉ cập nhật sau khi Manager duyệt.
                await _shiftClosingRepo
                    .SaveChangesAsync();

                return report.Id;
            }
        );
}

       /// <summary>
/// Manager duyệt báo cáo kết ca
/// và cập nhật tồn quầy theo số lượng thực tế.
/// </summary>
public async Task ApproveReportAsync(
    int managerId,
    int reportId)
{
    var manager =
        await GetValidManagerAsync(
            managerId
        );

    var managerBranchId =
        manager.BranchId!.Value;

    if (reportId <= 0)
    {
        throw new InvalidOperationException(
            "Mã báo cáo không hợp lệ."
        );
    }

    await _shiftClosingRepo
        .ExecuteInTransactionAsync(
            async () =>
            {
                // Lấy báo cáo kèm chi tiết
                // và thông tin sản phẩm.
                var report =
                    await _shiftClosingRepo
                        .GetReportForApprovalAsync(
                            reportId
                        );

                if (report == null)
                {
                    throw new InvalidOperationException(
                        "Không tìm thấy báo cáo kết ca."
                    );
                }

                if (
                    report.BranchId !=
                    managerBranchId
                )
                {
                    throw new InvalidOperationException(
                        "Bạn không được duyệt báo cáo của cơ sở khác."
                    );
                }

                var currentStatus =
                    NormalizeStatus(
                        report.Status,
                        StatusPending
                    );

                if (
                    currentStatus ==
                    StatusApproved
                )
                {
                    throw new InvalidOperationException(
                        "Báo cáo này đã được duyệt."
                    );
                }

                if (
                    currentStatus ==
                    StatusRejected
                )
                {
                    throw new InvalidOperationException(
                        "Báo cáo này đã bị từ chối. Nhân viên cần gửi lại trước khi duyệt."
                    );
                }

                if (
                    currentStatus !=
                    StatusPending
                )
                {
                    throw new InvalidOperationException(
                        "Trạng thái báo cáo không hợp lệ."
                    );
                }

                if (
                    report.KhoShiftClosingDetails
                        .Count == 0
                )
                {
                    throw new InvalidOperationException(
                        "Báo cáo không có chi tiết kiểm kê."
                    );
                }

                var productIds =
                    report.KhoShiftClosingDetails
                        .Select(detail =>
                            detail.ProductId
                        )
                        .Distinct()
                        .ToList();

                // Lấy tồn quầy có tracking
                // để cập nhật Quantity.
                var frontStocks =
                    await _shiftClosingRepo
                        .GetTrackedFrontStocksByProductIdsAsync(
                            report.BranchId,
                            productIds
                        );

                if (
                    frontStocks.Count !=
                    productIds.Count
                )
                {
                    throw new InvalidOperationException(
                        "Một số sản phẩm trong báo cáo không còn tồn tại tại quầy."
                    );
                }

                // Kiểm tra tồn quầy chưa bị thay đổi
                // kể từ lúc Staff gửi báo cáo.
                foreach (
                    var detail in
                    report.KhoShiftClosingDetails
                )
                {
                    var frontStock =
                        frontStocks.First(item =>
                            item.ProductId ==
                            detail.ProductId
                        );

                    var currentQuantity =
                        frontStock.Quantity ?? 0;

                    if (
                        currentQuantity !=
                        detail.SystemCount
                    )
                    {
                        var productName =
                            detail.Product
                                ?.ProductName ??
                            $"Sản phẩm #{detail.ProductId}";

                        throw new InvalidOperationException(
                            $"Tồn quầy của '{productName}' đã thay đổi sau khi nhân viên gửi báo cáo. " +
                            $"Khi gửi: {detail.SystemCount}, hiện tại: {currentQuantity}. " +
                            "Hãy từ chối báo cáo để nhân viên kiểm kê và gửi lại."
                        );
                    }
                }

                // Cập nhật tồn quầy bằng
                // số lượng thực tế đã kiểm kê.
                foreach (
                    var detail in
                    report.KhoShiftClosingDetails
                )
                {
                    var frontStock =
                        frontStocks.First(item =>
                            item.ProductId ==
                            detail.ProductId
                        );

                    frontStock.Quantity =
                        detail.ActualCount;
                }

                report.Status =
                    StatusApproved;

                report.ReviewedBy =
                    managerId;

                report.ReviewedAt =
                    DateTime.Now;

                report.RejectReason =
                    null;

                await _shiftClosingRepo
                    .SaveChangesAsync();
            }
        );
}

       /// <summary>
/// Manager từ chối báo cáo kết ca.
///
/// Từ chối không làm thay đổi tồn quầy.
/// Staff có thể sửa và gửi lại báo cáo.
/// </summary>
public async Task RejectReportAsync(
    int managerId,
    int reportId,
    string? reason)
{
    var manager =
        await GetValidManagerAsync(
            managerId
        );

    var managerBranchId =
        manager.BranchId!.Value;

    if (reportId <= 0)
    {
        throw new InvalidOperationException(
            "Mã báo cáo không hợp lệ."
        );
    }

    var normalizedReason =
        string.IsNullOrWhiteSpace(
            reason
        )
            ? null
            : reason.Trim();

    if (normalizedReason == null)
    {
        throw new InvalidOperationException(
            "Vui lòng nhập lý do từ chối."
        );
    }

    if (normalizedReason.Length > 500)
    {
        throw new InvalidOperationException(
            "Lý do từ chối không được vượt quá 500 ký tự."
        );
    }

    var report =
        await _shiftClosingRepo
            .GetReportByIdAsync(
                reportId
            );

    if (report == null)
    {
        throw new InvalidOperationException(
            "Không tìm thấy báo cáo kết ca."
        );
    }

    if (
        report.BranchId !=
        managerBranchId
    )
    {
        throw new InvalidOperationException(
            "Bạn không được từ chối báo cáo của cơ sở khác."
        );
    }

    var currentStatus =
        NormalizeStatus(
            report.Status,
            StatusPending
        );

    if (
        currentStatus ==
        StatusApproved
    )
    {
        throw new InvalidOperationException(
            "Báo cáo đã được duyệt nên không thể từ chối."
        );
    }

    if (
        currentStatus ==
        StatusRejected
    )
    {
        throw new InvalidOperationException(
            "Báo cáo này đã bị từ chối trước đó."
        );
    }

    if (
        currentStatus !=
        StatusPending
    )
    {
        throw new InvalidOperationException(
            "Trạng thái báo cáo không hợp lệ."
        );
    }

    report.Status =
        StatusRejected;

    report.ReviewedBy =
        managerId;

    report.ReviewedAt =
        DateTime.Now;

    report.RejectReason =
        normalizedReason;

    // Không cập nhật tồn quầy
    // khi từ chối báo cáo.
    await _shiftClosingRepo
        .SaveChangesAsync();
}

       /// <summary>
/// Lấy lịch sử báo cáo kết ca
/// của Staff đang đăng nhập.
/// </summary>
public async Task<List<ShiftClosingReportListDto>>
    GetMyReportsAsync(
        int staffId)
{
    // Kiểm tra Staff hợp lệ.
    await GetValidStaffAsync(
        staffId
    );

    // Lấy lịch sử thông qua Repository.
    return await _shiftClosingRepo
        .GetReportsByStaffIdAsync(
            staffId
        );
}

       /// <summary>
/// Lấy chi tiết một báo cáo kết ca
/// thuộc về Staff đang đăng nhập.
/// </summary>
public async Task<ShiftClosingReportDetailDto?>
    GetMyReportDetailAsync(
        int staffId,
        int reportId)
{
    // Kiểm tra Staff hợp lệ.
    await GetValidStaffAsync(
        staffId
    );

    if (reportId <= 0)
    {
        return null;
    }

    return await _shiftClosingRepo
        .GetReportDetailByStaffAsync(
            staffId,
            reportId
        );
}

       /// <summary>
/// Lấy danh sách báo cáo kết ca
/// dành cho Manager hoặc Admin.
/// </summary>
public async Task<List<ShiftClosingReportListDto>>
    GetReportsForManagementAsync(
        int? branchId)
{
    return await _shiftClosingRepo
        .GetReportsForManagementAsync(
            branchId
        );
}

       /// <summary>
/// Lấy chi tiết báo cáo kết ca
/// dành cho Manager hoặc Admin.
/// </summary>
public async Task<ShiftClosingReportDetailDto?>
    GetReportDetailForManagementAsync(
        int reportId,
        int? branchId)
{
    if (reportId <= 0)
    {
        return null;
    }

    return await _shiftClosingRepo
        .GetReportDetailForManagementAsync(
            reportId,
            branchId
        );
}

        /// <summary>
        /// Kiểm tra tài khoản có phải Staff hợp lệ
        /// và đã được gán cơ sở hay không.
        /// </summary>
        private async Task<NsUser>
            GetValidStaffAsync(
                int staffId)
        {
            if (staffId <= 0)
            {
                throw new InvalidOperationException(
                    "Không tìm thấy thông tin nhân viên."
                );
            }

            // Tìm người dùng và vai trò
            // thông qua Repository.
            var staff =
                await _shiftClosingRepo
                    .GetUserByIdAsync(
                        staffId
                    );

            if (staff == null)
            {
                throw new InvalidOperationException(
                    "Không tìm thấy tài khoản nhân viên."
                );
            }

            if (
                !staff.BranchId.HasValue ||
                staff.BranchId.Value <= 0
            )
            {
                throw new InvalidOperationException(
                    "Nhân viên chưa được gán cơ sở."
                );
            }

            var roleName =
                staff.Role?.RoleName
                    ?.Trim()
                    .ToUpperInvariant() ??
                string.Empty;

            var isStaff =
                roleName == "STAFF" ||
                roleName.Contains("NHÂN VIÊN") ||
                roleName.Contains("NHAN VIEN");

            if (!isStaff)
            {
                throw new InvalidOperationException(
                    "Chỉ nhân viên mới được báo cáo kết ca."
                );
            }

            return staff;
        }

        /// <summary>
        /// Kiểm tra tài khoản có phải Manager hợp lệ
        /// và đã được gán cơ sở hay không.
        /// </summary>
        private async Task<NsUser>
            GetValidManagerAsync(
                int managerId)
        {
            if (managerId <= 0)
            {
                throw new InvalidOperationException(
                    "Không tìm thấy thông tin Quản lý."
                );
            }

            // Tìm người dùng và vai trò
            // thông qua Repository.
            var manager =
                await _shiftClosingRepo
                    .GetUserByIdAsync(
                        managerId
                    );

            if (manager == null)
            {
                throw new InvalidOperationException(
                    "Không tìm thấy tài khoản Quản lý."
                );
            }

            if (
                !manager.BranchId.HasValue ||
                manager.BranchId.Value <= 0
            )
            {
                throw new InvalidOperationException(
                    "Quản lý chưa được gán cơ sở."
                );
            }

            var roleName =
                manager.Role?.RoleName
                    ?.Trim()
                    .ToUpperInvariant() ??
                string.Empty;

            var isManager =
                roleName == "MANAGER" ||
                roleName.Contains("QUẢN LÝ") ||
                roleName.Contains("QUAN LY");

            if (!isManager)
            {
                throw new InvalidOperationException(
                    "Chỉ Quản lý mới được duyệt báo cáo kết ca."
                );
            }

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

        

       

       
    }
}
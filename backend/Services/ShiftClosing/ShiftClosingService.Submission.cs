using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Repositories;

namespace LuanVanTotNghiep.Services
{
    public partial class ShiftClosingService
    {
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
    var reportId = await _shiftClosingRepo
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

    await _shiftDelegationService.LogActiveActionAsync(
        staffId,
        branchId,
        schedule.ShiftId,
        scheduleDate.Value,
        "SHIFT_CLOSING_REPORT_SUBMITTED",
        $"Đã lập báo cáo kết ca #{reportId}.");

    return reportId;
}

       /// <summary>
/// Manager duyệt báo cáo kết ca
/// và cập nhật tồn quầy theo số lượng thực tế.
/// </summary>
    }
}


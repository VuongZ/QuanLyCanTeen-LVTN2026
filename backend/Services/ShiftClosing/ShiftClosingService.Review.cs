using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Repositories;

namespace LuanVanTotNghiep.Services
{
    public partial class ShiftClosingService
    {
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
    }
}


using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Repositories;

namespace LuanVanTotNghiep.Services
{
    public partial class ShiftClosingService
    {
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
    }
}


using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Repositories;

namespace LuanVanTotNghiep.Services
{
    /// <summary>
    /// Xử lý nghiệp vụ xuất hàng
    /// từ kho chi nhánh ra quầy.
    ///
    /// Service chịu trách nhiệm:
    /// - Kiểm tra người thực hiện.
    /// - Kiểm tra lịch làm chính thức.
    /// - Kiểm tra khung giờ được phép xuất.
    /// - Kiểm tra số lượng tồn kho.
    /// - Điều phối tạo phiếu và cập nhật tồn kho.
    ///
    /// Luồng xử lý:
    /// Controller -> Service -> Repository -> Database.
    /// </summary>
    public partial class KhoExportService
    {
private async Task<NsUser>
            GetValidManagerAsync(
                int managerId,
                int branchId)
        {
            var manager =
                await _exportRepo.GetUserByIdAsync(
                    managerId
                );

            if (manager == null)
            {
                throw new InvalidOperationException(
                    "Không tìm thấy tài khoản quản lý."
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
                    "Chỉ quản lý chi nhánh mới được xuất hàng ra quầy."
                );
            }

            if (
                !manager.BranchId.HasValue ||
                manager.BranchId.Value <= 0
            )
            {
                throw new InvalidOperationException(
                    "Tài khoản quản lý chưa được gán chi nhánh."
                );
            }

            if (
                manager.BranchId.Value !=
                branchId
            )
            {
                throw new InvalidOperationException(
                    "Quản lý không thuộc chi nhánh đang xuất kho."
                );
            }

            return manager;
        }

        /// <summary>
        /// Kiểm tra lịch làm được chọn có hợp lệ
        /// và có nằm trong khung giờ xuất hàng hay không.
        /// </summary>
        private async Task
            ValidateScheduleForExportAsync(
                CreateExportTicketDto dto,
                NsUser manager)
        {
            var schedule =
                await _exportRepo
                    .GetScheduleByIdAndUserIdAsync(
                        dto.ScheduleId!.Value,
                        manager.Id
                    );

            if (schedule == null)
            {
                throw new InvalidOperationException(
                    "Không tìm thấy ca làm chính thức của quản lý."
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
                dto.BranchId
            )
            {
                throw new InvalidOperationException(
                    "Ca làm không thuộc chi nhánh đang xuất kho."
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

            if (scheduleDate != today)
            {
                throw new InvalidOperationException(
                    "Chỉ được xuất hàng cho ca làm trong ngày hiện tại."
                );
            }

            var now =
                DateTime.Now.TimeOfDay;

            var startTime =
                ToTimeSpan(
                    schedule.Shift.StartTime
                );

            var endTime =
                ToTimeSpan(
                    schedule.Shift.EndTime
                );

            if (
                !IsNowInExportWindow(
                    now,
                    startTime,
                    endTime
                )
            )
            {
                throw new InvalidOperationException(
                    $"Chỉ được xuất hàng trong thời gian ca làm hoặc trước ca tối đa {ExportPreparationMinutes} phút. " +
                    $"Ca này diễn ra từ {FormatTime(startTime)} đến {FormatTime(endTime)}."
                );
            }
        }

        /// <summary>
        /// Chuyển nhiều kiểu dữ liệu ngày
        /// về DateOnly.
        /// </summary>
        private static DateOnly?
            ToDateOnly(object? value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is DateOnly dateOnly)
            {
                return dateOnly;
            }

            if (value is DateTime dateTime)
            {
                return DateOnly.FromDateTime(
                    dateTime
                );
            }

            if (
                DateTime.TryParse(
                    value.ToString(),
                    out var parsedDate
                )
            )
            {
                return DateOnly.FromDateTime(
                    parsedDate
                );
            }

            return null;
        }

        /// <summary>
        /// Chuyển nhiều kiểu dữ liệu thời gian
        /// về TimeSpan.
        /// </summary>
        private static TimeSpan
            ToTimeSpan(object? value)
        {
            if (value == null)
            {
                return TimeSpan.Zero;
            }

            if (value is TimeOnly timeOnly)
            {
                return timeOnly.ToTimeSpan();
            }

            if (value is TimeSpan timeSpan)
            {
                return timeSpan;
            }

            if (value is DateTime dateTime)
            {
                return dateTime.TimeOfDay;
            }

            if (
                TimeSpan.TryParse(
                    value.ToString(),
                    out var parsedTime
                )
            )
            {
                return parsedTime;
            }

            return TimeSpan.Zero;
        }

        /// <summary>
        /// Kiểm tra thời gian hiện tại
        /// có nằm trong ca hay không.
        ///
        /// Hỗ trợ cả ca qua nửa đêm.
        /// </summary>
        private static bool IsNowInShift(
            TimeSpan now,
            TimeSpan start,
            TimeSpan end)
        {
            // Ca không đi qua nửa đêm.
            if (end >= start)
            {
                return
                    now >= start &&
                    now <= end;
            }

            // Ca đi qua nửa đêm.
            return
                now >= start ||
                now <= end;
        }

        /// <summary>
        /// Kiểm tra thời gian hiện tại có nằm trong
        /// khung giờ được phép xuất hàng hay không.
        ///
        /// Khung giờ bắt đầu trước ca 60 phút
        /// và kết thúc khi ca kết thúc.
        /// </summary>
        private static bool IsNowInExportWindow(
            TimeSpan now,
            TimeSpan start,
            TimeSpan end)
        {
            var allowedStart =
                start.Subtract(
                    TimeSpan.FromMinutes(
                        ExportPreparationMinutes
                    )
                );

            // Khung giờ không đi qua nửa đêm.
            if (end >= allowedStart)
            {
                return
                    now >= allowedStart &&
                    now <= end;
            }

            // Khung giờ đi qua nửa đêm.
            return
                now >= allowedStart ||
                now <= end;
        }

        /// <summary>
        /// Định dạng thời gian theo HH:mm.
        /// </summary>
        private static string FormatTime(
            TimeSpan time)
        {
            return
                $"{time.Hours:D2}:" +
                $"{time.Minutes:D2}";
        }
    }
}


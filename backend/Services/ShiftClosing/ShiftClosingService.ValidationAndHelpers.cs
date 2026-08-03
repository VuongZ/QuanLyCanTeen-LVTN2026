using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Repositories;

namespace LuanVanTotNghiep.Services
{
    public partial class ShiftClosingService
    {
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


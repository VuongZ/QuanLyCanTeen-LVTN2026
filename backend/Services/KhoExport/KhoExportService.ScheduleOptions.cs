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
public async Task<List<ExportScheduleOptionDto>>
            GetTodayExportSchedulesAsync(
                int managerId)
        {
            if (managerId <= 0)
            {
                throw new InvalidOperationException(
                    "Không tìm thấy thông tin quản lý."
                );
            }

            // Tìm tài khoản và vai trò của người dùng.
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

            // Chỉ Manager mới được xuất hàng ra quầy.
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

            // Manager phải được gán vào một chi nhánh.
            if (
                !manager.BranchId.HasValue ||
                manager.BranchId.Value <= 0
            )
            {
                throw new InvalidOperationException(
                    "Tài khoản quản lý chưa được gán chi nhánh."
                );
            }

            var today =
                DateOnly.FromDateTime(
                    DateTime.Today
                );

            var now =
                DateTime.Now.TimeOfDay;

            // Lấy lịch làm chính thức của Manager.
            var schedules =
                await _exportRepo
                    .GetSchedulesByUserIdAsync(
                        managerId
                    );

            return schedules
                // Chỉ lấy lịch làm trong ngày hiện tại.
                .Where(schedule =>
                    ToDateOnly(schedule.WorkDate) ==
                    today
                )

                // Lịch phải có thông tin ca.
                .Where(schedule =>
                    schedule.Shift != null
                )

                // Ca phải thuộc đúng chi nhánh
                // của Manager.
                .Where(schedule =>
                    schedule.Shift!.BranchId ==
                    manager.BranchId.Value
                )

                .Select(schedule =>
                {
                    var shift = schedule.Shift!;

                    var startTime =
                        ToTimeSpan(
                            shift.StartTime
                        );

                    var endTime =
                        ToTimeSpan(
                            shift.EndTime
                        );

                    var isInShift =
                        IsNowInShift(
                            now,
                            startTime,
                            endTime
                        );

                    var canExportNow =
                        IsNowInExportWindow(
                            now,
                            startTime,
                            endTime
                        );

                    return new ExportScheduleOptionDto
                    {
                        ScheduleId =
                            schedule.Id,

                        ShiftId =
                            schedule.ShiftId,

                        ShiftName =
                            shift.ShiftName ??
                            $"Ca #{schedule.ShiftId}",

                        WorkDate =
                            today.ToString(
                                "yyyy-MM-dd"
                            ),

                        StartTime =
                            FormatTime(startTime),

                        EndTime =
                            FormatTime(endTime),

                        CanExportNow =
                            canExportNow,

                        StatusLabel =
                            isInShift
                                ? "Đang trong ca"
                                : canExportNow
                                    ? $"Chuẩn bị trước ca {ExportPreparationMinutes} phút"
                                    : "Ngoài giờ ca"
                    };
                })

                // Chuỗi thời gian có dạng HH:mm
                // nên có thể sắp xếp trực tiếp.
                .OrderBy(schedule =>
                    schedule.StartTime
                )
                .ToList();
        }

        /// <summary>
        /// Tạo phiếu xuất hàng từ kho chi nhánh
        /// ra tồn quầy.
        ///
        /// Khi thành công:
        /// - Tạo phiếu xuất.
        /// - Tạo chi tiết phiếu.
        /// - Trừ số lượng trong kho.
        /// - Cộng số lượng vào tồn quầy.
        /// </summary>
    }
}


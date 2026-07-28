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
    public class KhoExportService
    {
        /// <summary>
        /// Cho phép chuẩn bị hàng trước giờ bắt đầu ca
        /// tối đa 60 phút.
        /// </summary>
        private const int ExportPreparationMinutes = 60;

        private readonly KhoExportRepo _exportRepo;

        /// <summary>
        /// Nhận KhoExportRepo thông qua
        /// Dependency Injection.
        /// </summary>
        public KhoExportService(
            KhoExportRepo exportRepo)
        {
            _exportRepo = exportRepo;
        }

        /// <summary>
        /// Lấy các ca làm trong ngày hiện tại
        /// mà Manager có thể chọn để xuất hàng.
        /// </summary>
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
        public async Task<int>
            CreateExportTicketAsync(
                CreateExportTicketDto dto)
        {
            // Kiểm tra người thực hiện.
            if (dto.ManagerId <= 0)
            {
                throw new InvalidOperationException(
                    "Không tìm thấy thông tin quản lý."
                );
            }

            // Kiểm tra chi nhánh xuất hàng.
            if (dto.BranchId <= 0)
            {
                throw new InvalidOperationException(
                    "Không tìm thấy thông tin chi nhánh."
                );
            }

            // Bắt buộc chọn lịch làm chính thức.
            if (
                !dto.ScheduleId.HasValue ||
                dto.ScheduleId.Value <= 0
            )
            {
                throw new InvalidOperationException(
                    "Vui lòng chọn ca làm cần xuất hàng ra quầy."
                );
            }

            // Phiếu phải có ít nhất một sản phẩm.
            if (
                dto.Items == null ||
                dto.Items.Count == 0
            )
            {
                throw new InvalidOperationException(
                    "Phiếu xuất không có sản phẩm nào."
                );
            }

            // Kiểm tra Manager và chi nhánh.
            var manager =
                await GetValidManagerAsync(
                    dto.ManagerId,
                    dto.BranchId
                );

            // Kiểm tra lịch làm và khung giờ.
            await ValidateScheduleForExportAsync(
                dto,
                manager
            );

            // Loại bỏ dòng không hợp lệ
            // và gộp sản phẩm bị lặp.
            var validItems = dto.Items
                .Where(item =>
                    item.ProductId > 0 &&
                    item.Quantity > 0
                )
                .GroupBy(item =>
                    item.ProductId
                )
                .Select(group =>
                    new ExportItemDto
                    {
                        ProductId =
                            group.Key,

                        Quantity =
                            group.Sum(item =>
                                item.Quantity
                            )
                    }
                )
                .ToList();

            if (validItems.Count == 0)
            {
                throw new InvalidOperationException(
                    "Danh sách hàng xuất không hợp lệ."
                );
            }

            // Toàn bộ thao tác tạo phiếu,
            // trừ kho và cộng tồn quầy
            // được thực hiện trong transaction.
            return await _exportRepo
                .ExecuteInTransactionAsync(
                    async () =>
                    {
                        // Tạo phiếu xuất.
                        var ticket =
                            new KhoExportTicket
                            {
                                ManagerId =
                                    dto.ManagerId,

                                BranchId =
                                    dto.BranchId,

                                ScheduleId =
                                    dto.ScheduleId,

                                ExportDate =
                                    DateTime.Now,

                                Note =
                                    string.IsNullOrWhiteSpace(
                                        dto.Note
                                    )
                                        ? null
                                        : dto.Note.Trim()
                            };

                        // Lưu phiếu trước để lấy ID.
                        await _exportRepo
                            .AddExportTicketAsync(
                                ticket
                            );

                        // Xử lý từng sản phẩm.
                        foreach (
                            var item in validItems
                        )
                        {
                            // Kiểm tra sản phẩm tồn tại.
                            var product =
                                await _exportRepo
                                    .GetProductByIdAsync(
                                        item.ProductId
                                    );

                            if (product == null)
                            {
                                throw new InvalidOperationException(
                                    $"Không tìm thấy sản phẩm có ID {item.ProductId}."
                                );
                            }

                            // Lấy tồn kho của sản phẩm
                            // tại chi nhánh.
                            var inventory =
                                await _exportRepo
                                    .GetBranchInventoryAsync(
                                        dto.BranchId,
                                        item.ProductId
                                    );

                            var currentWarehouseQuantity =
                                inventory?.Quantity ?? 0;

                            // Kiểm tra đủ số lượng xuất.
                            if (
                                inventory == null ||
                                currentWarehouseQuantity <
                                item.Quantity
                            )
                            {
                                throw new InvalidOperationException(
                                    $"Sản phẩm '{product.ProductName}' không đủ số lượng trong kho. " +
                                    $"Tồn hiện tại: {currentWarehouseQuantity}, " +
                                    $"cần xuất: {item.Quantity}."
                                );
                            }

                            // Tạo chi tiết phiếu xuất.
                            var detail =
                                new KhoExportDetail
                                {
                                    ExportId =
                                        ticket.Id,

                                    ProductId =
                                        item.ProductId,

                                    Quantity =
                                        item.Quantity
                                };

                            _exportRepo.AddExportDetail(
                                detail
                            );

                            // Trừ số lượng khỏi kho chi nhánh.
                            inventory.Quantity =
                                currentWarehouseQuantity -
                                item.Quantity;

                            // Tìm tồn quầy hiện tại.
                            var frontStock =
                                await _exportRepo
                                    .GetBranchFrontStockAsync(
                                        dto.BranchId,
                                        item.ProductId
                                    );

                            if (frontStock == null)
                            {
                                // Chưa có dòng tồn quầy:
                                // tạo mới.
                                frontStock =
                                    new KhoBranchFrontStock
                                    {
                                        BranchId =
                                            dto.BranchId,

                                        ProductId =
                                            item.ProductId,

                                        Quantity =
                                            item.Quantity
                                    };

                                _exportRepo
                                    .AddBranchFrontStock(
                                        frontStock
                                    );
                            }
                            else
                            {
                                // Đã có tồn quầy:
                                // cộng thêm số lượng xuất.
                                frontStock.Quantity =
                                    (frontStock.Quantity ?? 0) +
                                    item.Quantity;
                            }
                        }

                        // Lưu chi tiết phiếu,
                        // tồn kho và tồn quầy.
                        await _exportRepo
                            .SaveChangesAsync();

                        return ticket.Id;
                    }
                );
        }

        /// <summary>
        /// Lấy danh sách lịch sử phiếu xuất ra quầy.
        ///
        /// branchId null:
        /// lấy toàn hệ thống.
        ///
        /// branchId có giá trị:
        /// chỉ lấy một chi nhánh.
        /// </summary>
        public async Task<
            List<FrontStockExportTicketListDto>>
            GetFrontStockExportTicketsAsync(
                int? branchId)
        {
            return await _exportRepo
                .GetFrontStockExportTicketsAsync(
                    branchId
                );
        }

        /// <summary>
        /// Lấy chi tiết một phiếu xuất ra quầy.
        ///
        /// Khi branchId có giá trị,
        /// phiếu phải thuộc đúng chi nhánh đó.
        /// </summary>
        public async Task<
            FrontStockExportTicketDetailDto?>
            GetFrontStockExportTicketDetailAsync(
                int ticketId,
                int? branchId)
        {
            if (ticketId <= 0)
            {
                return null;
            }

            return await _exportRepo
                .GetFrontStockExportTicketDetailAsync(
                    ticketId,
                    branchId
                );
        }

        /// <summary>
        /// Kiểm tra người thực hiện có phải Manager
        /// và thuộc đúng chi nhánh hay không.
        /// </summary>
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
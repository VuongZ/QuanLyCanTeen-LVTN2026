using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Repositories
{
    /// <summary>
    /// Thực hiện các thao tác Database
    /// liên quan đến nghiệp vụ xuất hàng ra quầy.
    ///
    /// Repository chịu trách nhiệm:
    /// - Truy vấn người dùng và lịch làm.
    /// - Truy vấn sản phẩm và tồn kho.
    /// - Tạo phiếu xuất và chi tiết phiếu.
    /// - Cập nhật tồn kho và tồn quầy.
    /// - Lấy lịch sử phiếu xuất.
    /// - Quản lý transaction.
    /// </summary>
    public class KhoExportRepo
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// Nhận AppDbContext thông qua
        /// Dependency Injection.
        /// </summary>
        public KhoExportRepo(
            AppDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // NGƯỜI DÙNG VÀ LỊCH LÀM
        // =====================================================

        /// <summary>
        /// Tìm người dùng theo ID
        /// và lấy kèm thông tin vai trò.
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

        public async Task<bool> BranchIsActiveAsync(int branchId)
        {
            return await _context.DmBranches
                .AsNoTracking()
                .AnyAsync(branch =>
                    branch.Id == branchId &&
                    branch.IsActive);
        }

        /// <summary>
        /// Lấy các lịch làm chính thức
        /// được phân công cho một người dùng.
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
        /// Tìm một lịch làm chính thức
        /// theo ScheduleId và UserId.
        ///
        /// Điều kiện UserId giúp ngăn người dùng
        /// chọn lịch làm của tài khoản khác.
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

        // =====================================================
        // SẢN PHẨM VÀ TỒN KHO
        // =====================================================

        /// <summary>
        /// Tìm sản phẩm theo ID.
        /// </summary>
        public async Task<KhoProduct?>
            GetProductByIdAsync(
                int productId)
        {
            return await _context.KhoProducts
                .FirstOrDefaultAsync(product =>
                    product.Id == productId
                );
        }

        /// <summary>
        /// Tìm số lượng sản phẩm đang tồn
        /// trong kho của một chi nhánh.
        /// </summary>
        public async Task<KhoBranchInventory?>
            GetBranchInventoryAsync(
                int branchId,
                int productId)
        {
            return await _context
                .KhoBranchInventories
                .FirstOrDefaultAsync(inventory =>
                    inventory.BranchId ==
                        branchId &&
                    inventory.ProductId ==
                        productId
                );
        }

        /// <summary>
        /// Tìm số lượng sản phẩm đang tồn
        /// tại quầy của một chi nhánh.
        ///
        /// Kiểm tra cả Entity đang được theo dõi
        /// nhưng chưa SaveChanges để tránh tạo trùng.
        /// </summary>
        public async Task<KhoBranchFrontStock?>
            GetBranchFrontStockAsync(
                int branchId,
                int productId)
        {
            var trackedFrontStock =
                _context.KhoBranchFrontStocks
                    .Local
                    .FirstOrDefault(frontStock =>
                        frontStock.BranchId ==
                            branchId &&
                        frontStock.ProductId ==
                            productId
                    );

            if (trackedFrontStock != null)
            {
                return trackedFrontStock;
            }

            return await _context
                .KhoBranchFrontStocks
                .FirstOrDefaultAsync(frontStock =>
                    frontStock.BranchId ==
                        branchId &&
                    frontStock.ProductId ==
                        productId
                );
        }

        /// <summary>
        /// Thêm một dòng tồn quầy mới.
        ///
        /// Phương thức này chưa gọi SaveChanges.
        /// </summary>
        public void AddBranchFrontStock(
            KhoBranchFrontStock frontStock)
        {
            _context.KhoBranchFrontStocks.Add(
                frontStock
            );
        }

        // =====================================================
        // PHIẾU XUẤT VÀ CHI TIẾT PHIẾU
        // =====================================================

        /// <summary>
        /// Thêm phiếu xuất và lưu ngay
        /// để lấy được mã phiếu xuất.
        /// </summary>
        public async Task<KhoExportTicket>
            AddExportTicketAsync(
                KhoExportTicket ticket)
        {
            _context.KhoExportTickets.Add(
                ticket
            );

            await _context.SaveChangesAsync();

            return ticket;
        }

        /// <summary>
        /// Thêm một dòng chi tiết phiếu xuất.
        ///
        /// Phương thức này chưa gọi SaveChanges
        /// để có thể thêm nhiều dòng cùng lúc.
        /// </summary>
        public void AddExportDetail(
            KhoExportDetail detail)
        {
            _context.KhoExportDetails.Add(
                detail
            );
        }

        // =====================================================
        // LỊCH SỬ PHIẾU XUẤT
        // =====================================================

        /// <summary>
        /// Lấy danh sách lịch sử phiếu xuất ra quầy.
        ///
        /// branchId null:
        /// lấy phiếu của toàn hệ thống.
        ///
        /// branchId có giá trị:
        /// chỉ lấy phiếu thuộc chi nhánh đó.
        /// </summary>
        public async Task<
            List<FrontStockExportTicketListDto>>
            GetFrontStockExportTicketsAsync(
                int? branchId)
        {
            var query = _context.KhoExportTickets
                .AsNoTracking()
                .Include(ticket =>
                    ticket.Branch
                )
                .Include(ticket =>
                    ticket.Manager
                )
                .Include(ticket =>
                    ticket.Schedule
                )
                    .ThenInclude(schedule =>
                        schedule!.Shift
                    )
                .Include(ticket =>
                    ticket.KhoExportDetails
                )
                .AsQueryable();

            if (
                branchId.HasValue &&
                branchId.Value > 0
            )
            {
                query = query.Where(ticket =>
                    ticket.BranchId ==
                    branchId.Value
                );
            }

            var tickets = await query
                .OrderByDescending(ticket =>
                    ticket.Id
                )
                .ToListAsync();

            return tickets
                .Select(ticket =>
                    new FrontStockExportTicketListDto
                    {
                        Id = ticket.Id,

                        BranchId =
                            ticket.BranchId,

                        BranchName =
                            ticket.Branch?.Name ??
                            "Chưa rõ cơ sở",

                        ManagerName =
                            ticket.Manager?.FullName ??
                            "Chưa rõ người xuất",

                        ScheduleId =
                            ticket.ScheduleId,

                        ShiftName =
                            ticket.Schedule
                                ?.Shift
                                ?.ShiftName,

                        WorkDate =
                            FormatDate(
                                ticket.Schedule
                                    ?.WorkDate
                            ),

                        ShiftTime =
                            ticket.Schedule?.Shift ==
                            null
                                ? null
                                : FormatShiftTime(
                                    ticket.Schedule
                                        .Shift
                                        .StartTime,
                                    ticket.Schedule
                                        .Shift
                                        .EndTime
                                ),

                        ExportDate =
                            FormatDateTime(
                                ticket.ExportDate
                            ),

                        TotalQuantity =
                            ticket.KhoExportDetails
                                .Sum(detail =>
                                    detail.Quantity
                                ),

                        ItemCount =
                            ticket.KhoExportDetails
                                .Count,

                        Note = ticket.Note
                    }
                )
                .ToList();
        }

        /// <summary>
        /// Lấy chi tiết một phiếu xuất ra quầy.
        ///
        /// Khi branchId có giá trị, phiếu phải
        /// thuộc đúng chi nhánh đó.
        /// </summary>
        public async Task<
            FrontStockExportTicketDetailDto?>
            GetFrontStockExportTicketDetailAsync(
                int ticketId,
                int? branchId)
        {
            var query = _context.KhoExportTickets
                .AsNoTracking()
                .Include(ticket =>
                    ticket.Branch
                )
                .Include(ticket =>
                    ticket.Manager
                )
                .Include(ticket =>
                    ticket.Schedule
                )
                    .ThenInclude(schedule =>
                        schedule!.Shift
                    )
                .Include(ticket =>
                    ticket.KhoExportDetails
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
                query = query.Where(ticket =>
                    ticket.BranchId ==
                    branchId.Value
                );
            }

            var ticket = await query
                .FirstOrDefaultAsync(ticket =>
                    ticket.Id == ticketId
                );

            if (ticket == null)
            {
                return null;
            }

            return new FrontStockExportTicketDetailDto
            {
                Id = ticket.Id,

                BranchId = ticket.BranchId,

                BranchName =
                    ticket.Branch?.Name ??
                    "Chưa rõ cơ sở",

                ManagerName =
                    ticket.Manager?.FullName ??
                    "Chưa rõ người xuất",

                ScheduleId =
                    ticket.ScheduleId,

                ShiftName =
                    ticket.Schedule
                        ?.Shift
                        ?.ShiftName,

                WorkDate =
                    FormatDate(
                        ticket.Schedule?.WorkDate
                    ),

                ShiftTime =
                    ticket.Schedule?.Shift == null
                        ? null
                        : FormatShiftTime(
                            ticket.Schedule
                                .Shift
                                .StartTime,
                            ticket.Schedule
                                .Shift
                                .EndTime
                        ),

                ExportDate =
                    FormatDateTime(
                        ticket.ExportDate
                    ),

                TotalQuantity =
                    ticket.KhoExportDetails
                        .Sum(detail =>
                            detail.Quantity
                        ),

                ItemCount =
                    ticket.KhoExportDetails.Count,

                Note = ticket.Note,

                Items = ticket.KhoExportDetails
                    .Select(detail =>
                        new FrontStockExportTicketItemDto
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

                            Quantity =
                                detail.Quantity
                        }
                    )
                    .ToList()
            };
        }

        // =====================================================
        // LƯU DỮ LIỆU VÀ TRANSACTION
        // =====================================================

        /// <summary>
        /// Lưu toàn bộ thay đổi đang được
        /// theo dõi trong AppDbContext.
        /// </summary>
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Thực hiện một nhóm thao tác
        /// trong cùng một transaction.
        ///
        /// Thành công:
        /// commit toàn bộ thay đổi.
        ///
        /// Có lỗi:
        /// rollback toàn bộ thay đổi.
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

        // =====================================================
        // HÀM ĐỊNH DẠNG
        // =====================================================

        /// <summary>
        /// Định dạng ngày giờ theo kiểu Việt Nam.
        /// </summary>
        private static string FormatDateTime(
            DateTime? value)
        {
            if (!value.HasValue)
            {
                return string.Empty;
            }

            return value.Value.ToString(
                "dd/MM/yyyy HH:mm"
            );
        }

        /// <summary>
        /// Định dạng ngày làm việc.
        ///
        /// Sử dụng object để tương thích
        /// với cả DateOnly và DateTime.
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

        /// <summary>
        /// Định dạng khoảng thời gian của ca.
        /// </summary>
        private static string FormatShiftTime(
            object? startValue,
            object? endValue)
        {
            var startTime =
                ToTimeSpan(startValue);

            var endTime =
                ToTimeSpan(endValue);

            return
                $"{FormatTime(startTime)} - " +
                $"{FormatTime(endTime)}";
        }

        /// <summary>
        /// Chuyển nhiều kiểu dữ liệu thời gian
        /// về TimeSpan.
        /// </summary>
        private static TimeSpan ToTimeSpan(
            object? value)
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

using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Services
{
    
    public class KhoExportService
    {
        private const int ExportPreparationMinutes = 60;
        private readonly AppDbContext _context;

        public KhoExportService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<ExportScheduleOptionDto>> GetTodayExportSchedulesAsync(int managerId)
        {
            var manager = await _context.NsUsers
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == managerId);

            if (manager == null)
                throw new InvalidOperationException("Không tìm thấy tài khoản quản lý.");

            var roleName = manager.Role?.RoleName?.ToUpperInvariant() ?? "";
            var isManager =
                roleName == "MANAGER" ||
                roleName.Contains("QUẢN LÝ") ||
                roleName.Contains("QUAN LY");

            if (!isManager)
                throw new InvalidOperationException("Chỉ quản lý chi nhánh mới được xuất hàng ra quầy.");

            var today = DateOnly.FromDateTime(DateTime.Today);
            var now = DateTime.Now.TimeOfDay;

            var schedules = await _context.CaFinalSchedules
                .AsNoTracking()
                .Include(s => s.Shift)
                .Where(s => s.UserId == managerId)
                .ToListAsync();

            return schedules
                .Where(s => ToDateOnly(s.WorkDate) == today)
                .Where(s => s.Shift != null)
                .Where(s => s.Shift.BranchId == manager.BranchId)
                .Select(s =>
                {
                    var startTime = ToTimeSpan(s.Shift!.StartTime);
var endTime = ToTimeSpan(s.Shift.EndTime);

var isInShift = IsNowInShift(now, startTime, endTime);
var canExportNow = IsNowInExportWindow(now, startTime, endTime);

return new ExportScheduleOptionDto
{
    ScheduleId = s.Id,
    ShiftId = s.ShiftId,
    ShiftName = s.Shift.ShiftName ?? $"Ca #{s.ShiftId}",
    WorkDate = today.ToString("yyyy-MM-dd"),
    StartTime = FormatTime(startTime),
    EndTime = FormatTime(endTime),
    CanExportNow = canExportNow,
    StatusLabel = isInShift
        ? "Đang trong ca"
        : canExportNow
            ? $"Chuẩn bị trước ca {ExportPreparationMinutes} phút"
            : "Ngoài giờ ca"
};
                })
                .OrderBy(s => s.StartTime)
                .ToList();
        }

        public async Task<int> CreateExportTicketAsync(CreateExportTicketDto dto)
        {
            if (dto.ManagerId <= 0)
                throw new InvalidOperationException("Không tìm thấy thông tin quản lý.");

            if (dto.BranchId <= 0)
                throw new InvalidOperationException("Không tìm thấy thông tin chi nhánh.");

            if (!dto.ScheduleId.HasValue || dto.ScheduleId.Value <= 0)
                throw new InvalidOperationException("Vui lòng chọn ca làm cần xuất hàng ra quầy.");

            if (dto.Items == null || dto.Items.Count == 0)
                throw new InvalidOperationException("Phiếu xuất không có sản phẩm nào.");

            var manager = await GetValidManagerAsync(dto.ManagerId, dto.BranchId);

            await ValidateScheduleForExportAsync(dto, manager);

            var validItems = dto.Items
                .Where(i => i.ProductId > 0 && i.Quantity > 0)
                .GroupBy(i => i.ProductId)
                .Select(g => new ExportItemDto
                {
                    ProductId = g.Key,
                    Quantity = g.Sum(x => x.Quantity)
                })
                .ToList();

            if (validItems.Count == 0)
                throw new InvalidOperationException("Danh sách hàng xuất không hợp lệ.");

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var ticket = new KhoExportTicket
                {
                    ManagerId = dto.ManagerId,
                    BranchId = dto.BranchId,
                    ScheduleId = dto.ScheduleId,
                    ExportDate = DateTime.Now,
                    Note = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim()
                };

                _context.KhoExportTickets.Add(ticket);
                await _context.SaveChangesAsync();

                foreach (var item in validItems)
                {
                    var product = await _context.KhoProducts
                        .FirstOrDefaultAsync(p => p.Id == item.ProductId);

                    if (product == null)
                        throw new InvalidOperationException($"Không tìm thấy sản phẩm có ID {item.ProductId}.");

                    var inventory = await _context.KhoBranchInventories
                        .FirstOrDefaultAsync(i =>
                            i.BranchId == dto.BranchId &&
                            i.ProductId == item.ProductId);

                    var currentWarehouseQuantity = inventory == null
                        ? 0
                        : Convert.ToInt32(inventory.Quantity);

                    if (inventory == null || currentWarehouseQuantity < item.Quantity)
                    {
                        throw new InvalidOperationException(
                            $"Sản phẩm '{product.ProductName}' không đủ số lượng trong kho. " +
                            $"Tồn hiện tại: {currentWarehouseQuantity}, cần xuất: {item.Quantity}."
                        );
                    }

                    var detail = new KhoExportDetail
                    {
                        ExportId = ticket.Id,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity
                    };

                    _context.KhoExportDetails.Add(detail);

                    inventory.Quantity = currentWarehouseQuantity - item.Quantity;

                    var frontStock = await _context.KhoBranchFrontStocks
                        .FirstOrDefaultAsync(f =>
                            f.BranchId == dto.BranchId &&
                            f.ProductId == item.ProductId);

                    if (frontStock == null)
                    {
                        frontStock = new KhoBranchFrontStock
                        {
                            BranchId = dto.BranchId,
                            ProductId = item.ProductId,
                            Quantity = item.Quantity
                        };

                        _context.KhoBranchFrontStocks.Add(frontStock);
                    }
                    else
                    {
                        var currentFrontQuantity = Convert.ToInt32(frontStock.Quantity);
                        frontStock.Quantity = currentFrontQuantity + item.Quantity;
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return ticket.Id;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task<NsUser> GetValidManagerAsync(int managerId, int branchId)
        {
            var manager = await _context.NsUsers
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == managerId);

            if (manager == null)
                throw new InvalidOperationException("Không tìm thấy tài khoản quản lý.");

            var roleName = manager.Role?.RoleName?.ToUpperInvariant() ?? "";
            var isManager =
                roleName == "MANAGER" ||
                roleName.Contains("QUẢN LÝ") ||
                roleName.Contains("QUAN LY");

            if (!isManager)
                throw new InvalidOperationException("Chỉ quản lý chi nhánh mới được xuất hàng ra quầy.");

            if (manager.BranchId != branchId)
                throw new InvalidOperationException("Quản lý không thuộc chi nhánh đang xuất kho.");

            return manager;
        }

        private async Task ValidateScheduleForExportAsync(CreateExportTicketDto dto, NsUser manager)
        {
            var schedule = await _context.CaFinalSchedules
                .Include(s => s.Shift)
                .FirstOrDefaultAsync(s =>
                    s.Id == dto.ScheduleId!.Value &&
                    s.UserId == manager.Id);

            if (schedule == null)
                throw new InvalidOperationException("Không tìm thấy ca làm chính thức của quản lý.");

            if (schedule.Shift == null)
                throw new InvalidOperationException("Ca làm không hợp lệ.");

            if (schedule.Shift.BranchId != dto.BranchId)
                throw new InvalidOperationException("Ca làm không thuộc chi nhánh đang xuất kho.");

            var scheduleDate = ToDateOnly(schedule.WorkDate);
            var today = DateOnly.FromDateTime(DateTime.Today);

            if (scheduleDate != today)
                throw new InvalidOperationException("Chỉ được xuất hàng cho ca làm trong ngày hiện tại.");

            var now = DateTime.Now.TimeOfDay;
            var startTime = ToTimeSpan(schedule.Shift.StartTime);
            var endTime = ToTimeSpan(schedule.Shift.EndTime);

            if (!IsNowInExportWindow(now, startTime, endTime))
{
    throw new InvalidOperationException(
        $"Chỉ được xuất hàng trong thời gian ca làm hoặc trước ca tối đa {ExportPreparationMinutes} phút. " +
        $"Ca này diễn ra từ {FormatTime(startTime)} đến {FormatTime(endTime)}."
    );
}
        }

        private static DateOnly? ToDateOnly(object? value)
        {
            if (value == null) return null;

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
            if (value == null) return TimeSpan.Zero;

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

        private static bool IsNowInShift(TimeSpan now, TimeSpan start, TimeSpan end)
{
    if (end >= start)
    {
        return now >= start && now <= end;
    }

    return now >= start || now <= end;
}

private static bool IsNowInExportWindow(TimeSpan now, TimeSpan start, TimeSpan end)
{
    var allowedStart = start.Subtract(TimeSpan.FromMinutes(ExportPreparationMinutes));

    if (end >= allowedStart)
    {
        return now >= allowedStart && now <= end;
    }

    return now >= allowedStart || now <= end;
}

private static string FormatTime(TimeSpan time)
{
    return $"{time.Hours:D2}:{time.Minutes:D2}";
}
    }
}
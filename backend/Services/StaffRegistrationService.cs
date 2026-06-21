using LuanVanTotNghiep.Models.Entities;
using LuanVanTotNghiep.DTOs;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Services;

public class StaffRegistrationService
{
    private readonly AppDbContext _context;

    public StaffRegistrationService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CaStaffRegistration> RegisterAsync(RegisterShiftDto dto)
    {
       // 1. RÀO 1: Đợt đăng ký có tồn tại và đang OPEN không?
        var period = await _context.CaSchedulePeriods.FindAsync(dto.PeriodId);
        if (period == null || period.Status != "OPEN")
            throw new Exception("Đợt đăng ký không tồn tại hoặc đã đóng.");

        // 2. RÀO 2: Ngày làm việc có nằm trong thời gian của Đợt này không?
        if (dto.WorkDate < period.StartDate || dto.WorkDate > period.EndDate)
            throw new Exception("Ngày đăng ký không nằm trong thời gian của đợt này.");

        // 3. Quy đổi ngày ra Thứ và kiểm tra Config (Chỉ kiểm tra xem ca đó có mở không)
        var dayOfWeekString = dto.WorkDate.DayOfWeek.ToString();
        var config = await _context.CaBranchShiftConfigs
            .FirstOrDefaultAsync(c => c.ShiftId == dto.ShiftId && c.DayOfWeek == dayOfWeekString);

        if (config == null || config.MaxStaff <= 0)
            throw new Exception("Ca làm này không mở vào ngày bạn chọn.");

        // ĐÃ XÓA RÀO 3: Không đếm số lượng nữa, ai cũng được đăng ký!

        // 4. RÀO 4: Chỉ chặn không cho 1 người spam bấm 2 lần vào cùng 1 ca trong 1 ngày
        var isDuplicate = await _context.CaStaffRegistrations
            .AnyAsync(r => r.UserId == dto.UserId && r.ShiftId == dto.ShiftId && r.WorkDate == dto.WorkDate);
        if (isDuplicate)
            throw new Exception("Bạn đã đăng ký ca này vào ngày này rồi!");

        // 5. Lưu nguyện vọng vào DB
        var registration = new CaStaffRegistration
        {
            UserId = dto.UserId,
            PeriodId = dto.PeriodId,
            ShiftId = dto.ShiftId,
            WorkDate = dto.WorkDate,
            Status = "Chờ Duyệt" // Mọi người đều phải chờ Manager quyết định
        };

        _context.CaStaffRegistrations.Add(registration);
        await _context.SaveChangesAsync();

        return registration;
    }

   // 4. NHÂN VIÊN: Xem lịch làm việc cá nhân (Lấy toàn bộ trạng thái)
    public async Task<IEnumerable<CaStaffRegistration>> GetMyScheduleAsync(int userId, int periodId)
    {
        return await _context.CaStaffRegistrations
            .Include(r => r.Shift) 
            .Where(r => r.UserId == userId 
                     && r.PeriodId == periodId) // 👉 ĐÃ XÓA ĐIỀU KIỆN "Đã Duyệt" Ở ĐÂY
            .OrderBy(r => r.WorkDate)
            .ToListAsync();
    }

    // 1. MANAGER: Lấy danh sách nguyện vọng của 1 Đợt (Sắp xếp theo thứ tự ai nộp trước đứng trên)
    public async Task<IEnumerable<CaStaffRegistration>> GetRegistrationsByPeriodAsync(int periodId)
    {
        return await _context.CaStaffRegistrations
            .Include(r => r.User) // Kéo theo thông tin User để lấy Tên nhân viên
            .Include(r => r.Shift) // Kéo theo thông tin Ca làm
            .Where(r => r.PeriodId == periodId)
            // Mẹo cực hay: ID tăng dần tự động tương đương với việc ai đăng ký trước đứng trước!
            .OrderBy(r => r.WorkDate)
            .ThenBy(r => r.Id) 
            .ToListAsync();
    }

    // 2. MANAGER: Duyệt hoặc Từ chối 1 người
    public async Task UpdateStatusAsync(int registrationId, string newStatus)
    {
        var registration = await _context.CaStaffRegistrations.FindAsync(registrationId);
        if (registration == null)
            throw new Exception("Không tìm thấy phiếu đăng ký này.");

        // Chỉ cho phép nhập 2 trạng thái này để tránh rác DB
        if (newStatus != "Đã Duyệt" && newStatus != "Từ Chối" && newStatus != "Chờ Duyệt")
            throw new ArgumentException("Trạng thái không hợp lệ.");

        registration.Status = newStatus;
        await _context.SaveChangesAsync();
    }

    // 3. MANAGER: Xuất bản lịch làm việc chính thức (Chốt sổ)
    public async Task PublishScheduleAsync(PublishScheduleDto dto)
    {
        // 1. Chuyển trạng thái của Đợt thành PUBLISHED (Đã chốt)
        var period = await _context.CaSchedulePeriods.FindAsync(dto.PeriodId);
        if (period == null) 
            throw new Exception("Không tìm thấy đợt đăng ký này.");
            
        period.Status = "PUBLISHED";

        // 2. Lấy toàn bộ phiếu đăng ký của đợt này lên
        var allRegistrations = await _context.CaStaffRegistrations
            .Where(r => r.PeriodId == dto.PeriodId)
            .ToListAsync();

        // 3. Quét 1 vòng: Ai có ID nằm trong danh sách gửi lên -> Nhận job. Còn lại -> Rớt.
        foreach (var reg in allRegistrations)
        {
            if (dto.ApprovedRegistrationIds.Contains(reg.Id))
            {
                reg.Status = "Đã Duyệt";
            }
            else
            {
                reg.Status = "Từ Chối";
            }
        }

        // Lưu toàn bộ thay đổi (Cập nhật Status của Period và Registration cùng lúc)
        await _context.SaveChangesAsync();
    }
    // 5. NHÂN VIÊN: Hủy ca đã đăng ký (Chỉ được hủy khi chưa duyệt)
    public async Task CancelRegistrationAsync(int id, int userId)
    {
        var reg = await _context.CaStaffRegistrations.FindAsync(id);
        if (reg == null) 
            throw new Exception("Không tìm thấy phiếu đăng ký này.");
            
        if (reg.UserId != userId) 
            throw new Exception("Bạn không có quyền xóa ca của người khác.");
            
        if (reg.Status != "Chờ Duyệt") 
            throw new Exception("Ca này đã được quản lý xử lý, không thể hủy.");

        _context.CaStaffRegistrations.Remove(reg);
        await _context.SaveChangesAsync();
    }
}
using System;
using System.Collections.Generic;

namespace LuanVanTotNghiep.backend.Models.Entities;

public partial class CaStaffRegistration
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int ShiftId { get; set; }

    public DateOnly WorkDate { get; set; }

    // REGISTERED:
    // Giữ vị trí chính thức trong ca.
    //
    // WAITLIST:
    // Đăng ký dự phòng khi ca đã đủ người.
    //
    // CANCELLED:
    // Nhân viên đã hủy đăng ký.
    //
    // REPLACEMENT_SELECTED:
    // Đã được Manager chọn vào thay ca khẩn cấp.
    public string Status { get; set; } = null!;

    public int? PeriodId { get; set; }

    // Thời điểm đăng ký, dùng để sắp xếp danh sách chờ.
    public DateTime RegisteredAt { get; set; }

    public virtual CaSchedulePeriod? Period { get; set; }

    public virtual CaShift Shift { get; set; } = null!;

    public virtual NsUser User { get; set; } = null!;
}
using System;
using System.Collections.Generic;

namespace LuanVanTotNghiep.backend.Models.Entities;

public partial class CaFinalSchedule
{
    public int Id { get; set; }

    // Đợt đăng ký tạo ra lịch này.
    public int? PeriodId { get; set; }

    // Phiếu đăng ký dùng để tạo lịch.
    // Lịch của Manager không có phiếu đăng ký nên có thể NULL.
    public int? SourceRegistrationId { get; set; }

    public int UserId { get; set; }

    public int ShiftId { get; set; }

    public DateOnly WorkDate { get; set; }

    // DRAFT
    // PUBLISHED
    // LEAVE_APPROVED
    // ABSENT
    // CANCELLED
    public string Status { get; set; } = null!;

    // NORMAL
    // EMERGENCY_REPLACEMENT
    public string AssignmentType { get; set; } = null!;

    // Hệ số riêng của lịch:
    // bình thường = 1.00
    // thay khẩn cấp = 1.50 hoặc theo quy tắc lương.
    public decimal PayMultiplier { get; set; }

    // ID lịch của người mà lịch này đang thay thế.
    public int? ReplacesScheduleId { get; set; }

    // Lý do nghỉ có phép hoặc vắng không phép.
    public string? AbsenceReason { get; set; }

    // Manager ghi nhận nghỉ/vắng.
    public int? AbsenceMarkedByUserId { get; set; }

    public DateTime? AbsenceMarkedAt { get; set; }

    // Manager điều động người thay.
    public int? AssignedByUserId { get; set; }

    public DateTime? AssignedAt { get; set; }

    public virtual ICollection<CaAttendance>
        CaAttendances { get; set; } =
        new List<CaAttendance>();

    public virtual ICollection<KhoExportTicket>
        KhoExportTickets { get; set; } =
        new List<KhoExportTicket>();

    public virtual KhoShiftClosingReport?
        KhoShiftClosingReport { get; set; }

    public virtual CaShift Shift { get; set; } = null!;

    public virtual NsUser User { get; set; } = null!;

    public virtual CaSchedulePeriod? Period { get; set; }

    public virtual CaStaffRegistration?
        SourceRegistration { get; set; }

    // Với lịch thay:
    // ReplacesSchedule là lịch của người nghỉ/vắng.
    public virtual CaFinalSchedule?
        ReplacesSchedule { get; set; }

    // Với lịch người nghỉ/vắng:
    // ReplacementSchedule là lịch của người đến thay.
    public virtual CaFinalSchedule?
        ReplacementSchedule { get; set; }

    public virtual NsUser?
        AbsenceMarkedByUser { get; set; }

    public virtual NsUser?
        AssignedByUser { get; set; }
}
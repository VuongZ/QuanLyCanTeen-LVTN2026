using System;
using System.Collections.Generic;

namespace LuanVanTotNghiep.backend.Models.Entities;

public partial class CaFinalSchedule
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int ShiftId { get; set; }

    public DateOnly WorkDate { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<CaAttendance> CaAttendances { get; set; } = new List<CaAttendance>();

    public virtual ICollection<KhoExportTicket> KhoExportTickets { get; set; } = new List<KhoExportTicket>();

    public virtual KhoShiftClosingReport? KhoShiftClosingReport { get; set; }

    public virtual CaShift Shift { get; set; } = null!;

    public virtual NsUser User { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace LuanVanTotNghiep.backend.Models.Entities;

public partial class KhoShiftClosingReport
{
    public int Id { get; set; }

    public int BranchId { get; set; }

    public int UserId { get; set; }

    public int? ScheduleId { get; set; }

    public DateTime? ReportDate { get; set; }

    public string? Note { get; set; }

    public virtual DmBranch Branch { get; set; } = null!;

    public virtual ICollection<KhoShiftClosingDetail> KhoShiftClosingDetails { get; set; } = new List<KhoShiftClosingDetail>();

    public virtual CaFinalSchedule? Schedule { get; set; }

    public virtual NsUser User { get; set; } = null!;
}

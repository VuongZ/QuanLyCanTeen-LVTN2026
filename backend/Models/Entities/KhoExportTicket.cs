using System;
using System.Collections.Generic;

namespace LuanVanTotNghiep.backend.Models.Entities;

public partial class KhoExportTicket
{
    public int Id { get; set; }

    public int ManagerId { get; set; }

    public int BranchId { get; set; }

    public int? ScheduleId { get; set; }

    public DateTime? ExportDate { get; set; }

    public string? Note { get; set; }

    public virtual DmBranch Branch { get; set; } = null!;

    public virtual ICollection<KhoExportDetail> KhoExportDetails { get; set; } = new List<KhoExportDetail>();

    public virtual NsUser Manager { get; set; } = null!;

    public virtual CaFinalSchedule? Schedule { get; set; }
}

using System;
using System.Collections.Generic;

namespace LuanVanTotNghiep.Models.Entities;

public partial class KhoShiftClosingDetail
{
    public int Id { get; set; }

    public int ReportId { get; set; }

    public int ProductId { get; set; }

    public int ActualCount { get; set; }

    public virtual KhoProduct Product { get; set; } = null!;

    public virtual KhoShiftClosingReport Report { get; set; } = null!;
}

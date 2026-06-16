using System;
using System.Collections.Generic;

namespace LuanVanTotNghiep.Models.Entities;

public partial class KhoExportDetail
{
    public int Id { get; set; }

    public int ExportId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public virtual KhoExportTicket Export { get; set; } = null!;

    public virtual KhoProduct Product { get; set; } = null!;
}

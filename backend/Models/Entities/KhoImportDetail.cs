using System;
using System.Collections.Generic;

namespace LuanVanTotNghiep.backend.Models.Entities;

public partial class KhoImportDetail
{
    public int Id { get; set; }

    public int ImportId { get; set; }

    public int ProductId { get; set; }

    public string? UnitAtTime { get; set; }

    public int Quantity { get; set; }

    public decimal? UnitPrice { get; set; }

    public virtual KhoImportTicket Import { get; set; } = null!;

    public virtual KhoProduct Product { get; set; } = null!;
}

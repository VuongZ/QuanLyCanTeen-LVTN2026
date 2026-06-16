using System;
using System.Collections.Generic;

namespace LuanVanTotNghiep.Models.Entities;

public partial class KhoBranchFrontStock
{
    public int Id { get; set; }

    public int BranchId { get; set; }

    public int ProductId { get; set; }

    public int? Quantity { get; set; }

    public virtual DmBranch Branch { get; set; } = null!;

    public virtual KhoProduct Product { get; set; } = null!;
}

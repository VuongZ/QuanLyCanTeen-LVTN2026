using System;
using System.Collections.Generic;

namespace LuanVanTotNghiep.backend.Models.Entities;

public partial class KhoBranchInventory
{
    public int Id { get; set; }

    public int BranchId { get; set; }

    public int ProductId { get; set; }

    public int? Quantity { get; set; }

    public virtual DmBranch Branch { get; set; } = null!;

    public virtual KhoProduct Product { get; set; } = null!;
}

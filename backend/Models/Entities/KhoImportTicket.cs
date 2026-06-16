using System;
using System.Collections.Generic;

namespace LuanVanTotNghiep.Models.Entities;

public partial class KhoImportTicket
{
    public int Id { get; set; }

    public int ManagerId { get; set; }

    public int BranchId { get; set; }

    public int SupplierId { get; set; }

    public DateTime? ImportDate { get; set; }

    public virtual DmBranch Branch { get; set; } = null!;

    public virtual ICollection<KhoImportDetail> KhoImportDetails { get; set; } = new List<KhoImportDetail>();

    public virtual NsUser Manager { get; set; } = null!;

    public virtual KhoSupplier Supplier { get; set; } = null!;
}

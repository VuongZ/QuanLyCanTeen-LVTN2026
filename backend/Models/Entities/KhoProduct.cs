using System;
using System.Collections.Generic;

namespace LuanVanTotNghiep.backend.Models.Entities;

public partial class KhoProduct
{
    public int Id { get; set; }

    public string? ProductCode { get; set; }

    public string ProductName { get; set; } = null!;

    public string? Unit { get; set; }

    public int? SupplierId { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? InactiveAt { get; set; }

    public int? InactiveBy { get; set; }

    public string? InactiveReason { get; set; }

    public virtual NsUser? InactiveByNavigation { get; set; }

    public virtual ICollection<KhoBranchFrontStock> KhoBranchFrontStocks { get; set; } = new List<KhoBranchFrontStock>();

    public virtual ICollection<KhoBranchInventory> KhoBranchInventories { get; set; } = new List<KhoBranchInventory>();

    public virtual ICollection<KhoExportDetail> KhoExportDetails { get; set; } = new List<KhoExportDetail>();

    public virtual ICollection<KhoImportDetail> KhoImportDetails { get; set; } = new List<KhoImportDetail>();

    public virtual ICollection<KhoShiftClosingDetail> KhoShiftClosingDetails { get; set; } = new List<KhoShiftClosingDetail>();

    public virtual KhoSupplier? Supplier { get; set; }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Models.Entities;

[Table("kho_branch_front_stock")]
[Index("BranchId", Name = "fk_front_branch")]
[Index("ProductId", Name = "fk_front_product")]
[MySqlCollation("utf8mb4_unicode_ci")]
public partial class KhoBranchFrontStock
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("branch_id")]
    public int BranchId { get; set; }

    [Column("product_id")]
    public int ProductId { get; set; }

    [Column("quantity")]
    public int? Quantity { get; set; }

    [ForeignKey("BranchId")]
    [InverseProperty("KhoBranchFrontStocks")]
    public virtual DmBranch Branch { get; set; } = null!;

    [InverseProperty("FrontStock")]
    public virtual ICollection<KhoExportDetail> KhoExportDetails { get; set; } = new List<KhoExportDetail>();

    [InverseProperty("FrontStock")]
    public virtual ICollection<KhoShiftClosingDetail> KhoShiftClosingDetails { get; set; } = new List<KhoShiftClosingDetail>();

    [ForeignKey("ProductId")]
    [InverseProperty("KhoBranchFrontStocks")]
    public virtual KhoProduct Product { get; set; } = null!;
}

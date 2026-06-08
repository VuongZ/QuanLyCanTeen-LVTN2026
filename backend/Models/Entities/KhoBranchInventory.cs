using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Models.Entities;

[Table("kho_branch_inventory")]
[Index("BranchId", Name = "branch_id")]
[Index("ProductId", Name = "product_id")]
[MySqlCollation("utf8mb4_unicode_ci")]
public partial class KhoBranchInventory
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
    [InverseProperty("KhoBranchInventories")]
    public virtual DmBranch Branch { get; set; } = null!;

    [InverseProperty("Inventory")]
    public virtual ICollection<KhoExportDetail> KhoExportDetails { get; set; } = new List<KhoExportDetail>();

    [InverseProperty("Inventory")]
    public virtual ICollection<KhoImportDetail> KhoImportDetails { get; set; } = new List<KhoImportDetail>();

    [ForeignKey("ProductId")]
    [InverseProperty("KhoBranchInventories")]
    public virtual KhoProduct Product { get; set; } = null!;
}

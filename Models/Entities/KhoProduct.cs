using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Models.Entities;

[Table("kho_product")]
[Index("SupplierId", Name = "fk_product_supplier")]
[MySqlCollation("utf8mb4_unicode_ci")]
public partial class KhoProduct
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("product_name")]
    [StringLength(255)]
    public string ProductName { get; set; } = null!;

    [Column("unit")]
    [StringLength(50)]
    public string? Unit { get; set; }

    [Column("supplier_id")]
    public int? SupplierId { get; set; }

    [InverseProperty("Product")]
    public virtual ICollection<KhoBranchFrontStock> KhoBranchFrontStocks { get; set; } = new List<KhoBranchFrontStock>();

    [InverseProperty("Product")]
    public virtual ICollection<KhoBranchInventory> KhoBranchInventories { get; set; } = new List<KhoBranchInventory>();

    [ForeignKey("SupplierId")]
    [InverseProperty("KhoProducts")]
    public virtual KhoSupplier? Supplier { get; set; }
}

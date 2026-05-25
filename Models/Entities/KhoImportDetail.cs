using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Models.Entities;

[Table("kho_import_detail")]
[Index("ImportId", Name = "fk_impdet_import")]
[Index("InventoryId", Name = "fk_impdet_inventory")]
[MySqlCollation("utf8mb4_unicode_ci")]
public partial class KhoImportDetail
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("import_id")]
    public int ImportId { get; set; }

    [Column("inventory_id")]
    public int InventoryId { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("unit_price")]
    [Precision(10, 2)]
    public decimal? UnitPrice { get; set; }

    [ForeignKey("ImportId")]
    [InverseProperty("KhoImportDetails")]
    public virtual KhoImportTicket Import { get; set; } = null!;

    [ForeignKey("InventoryId")]
    [InverseProperty("KhoImportDetails")]
    public virtual KhoBranchInventory Inventory { get; set; } = null!;
}

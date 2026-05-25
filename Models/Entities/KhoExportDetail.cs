using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Models.Entities;

[Table("kho_export_detail")]
[Index("ExportId", Name = "fk_det_export")]
[Index("FrontStockId", Name = "fk_expdet_frontstock")]
[Index("InventoryId", Name = "fk_expdet_inventory")]
[MySqlCollation("utf8mb4_unicode_ci")]
public partial class KhoExportDetail
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("export_id")]
    public int ExportId { get; set; }

    [Column("inventory_id")]
    public int InventoryId { get; set; }

    [Column("front_stock_id")]
    public int FrontStockId { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [ForeignKey("ExportId")]
    [InverseProperty("KhoExportDetails")]
    public virtual KhoExportTicket Export { get; set; } = null!;

    [ForeignKey("FrontStockId")]
    [InverseProperty("KhoExportDetails")]
    public virtual KhoBranchFrontStock FrontStock { get; set; } = null!;

    [ForeignKey("InventoryId")]
    [InverseProperty("KhoExportDetails")]
    public virtual KhoBranchInventory Inventory { get; set; } = null!;
}

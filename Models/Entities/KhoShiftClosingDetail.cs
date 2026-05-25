using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Models.Entities;

[Table("kho_shift_closing_detail")]
[Index("FrontStockId", Name = "fk_detail_frontstock")]
[Index("ReportId", Name = "fk_detail_report")]
[MySqlCollation("utf8mb4_unicode_ci")]
public partial class KhoShiftClosingDetail
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("report_id")]
    public int ReportId { get; set; }

    [Column("front_stock_id")]
    public int FrontStockId { get; set; }

    [Column("actual_count")]
    public int ActualCount { get; set; }

    [Column("report_date", TypeName = "datetime")]
    public DateTime? ReportDate { get; set; }

    [ForeignKey("FrontStockId")]
    [InverseProperty("KhoShiftClosingDetails")]
    public virtual KhoBranchFrontStock FrontStock { get; set; } = null!;

    [ForeignKey("ReportId")]
    [InverseProperty("KhoShiftClosingDetails")]
    public virtual KhoShiftClosingReport Report { get; set; } = null!;
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Models.Entities;

[Table("kho_shift_closing_report")]
[Index("BranchId", Name = "fk_report_branch")]
[Index("UserId", Name = "fk_report_user")]
[MySqlCollation("utf8mb4_unicode_ci")]
public partial class KhoShiftClosingReport
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("branch_id")]
    public int BranchId { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("report_date", TypeName = "datetime")]
    public DateTime? ReportDate { get; set; }

    [ForeignKey("BranchId")]
    [InverseProperty("KhoShiftClosingReports")]
    public virtual DmBranch Branch { get; set; } = null!;

    [InverseProperty("Report")]
    public virtual ICollection<KhoShiftClosingDetail> KhoShiftClosingDetails { get; set; } = new List<KhoShiftClosingDetail>();

    [ForeignKey("UserId")]
    [InverseProperty("KhoShiftClosingReports")]
    public virtual NsUser User { get; set; } = null!;
}

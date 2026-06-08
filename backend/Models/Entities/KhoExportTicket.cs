using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Models.Entities;

[Table("kho_export_ticket")]
[Index("BranchId", Name = "fk_exp_branch")]
[Index("ManagerId", Name = "fk_exp_manager")]
[MySqlCollation("utf8mb4_unicode_ci")]
public partial class KhoExportTicket
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("manager_id")]
    public int ManagerId { get; set; }

    [Column("branch_id")]
    public int BranchId { get; set; }

    [Column("export_date", TypeName = "datetime")]
    public DateTime? ExportDate { get; set; }

    [ForeignKey("BranchId")]
    [InverseProperty("KhoExportTickets")]
    public virtual DmBranch Branch { get; set; } = null!;

    [InverseProperty("Export")]
    public virtual ICollection<KhoExportDetail> KhoExportDetails { get; set; } = new List<KhoExportDetail>();

    [ForeignKey("ManagerId")]
    [InverseProperty("KhoExportTickets")]
    public virtual NsUser Manager { get; set; } = null!;
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Models.Entities;

[Table("kho_import_ticket")]
[Index("BranchId", Name = "fk_imp_branch")]
[Index("ManagerId", Name = "fk_imp_manager")]
[Index("SupplierId", Name = "fk_imp_supplier")]
[MySqlCollation("utf8mb4_unicode_ci")]
public partial class KhoImportTicket
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("manager_id")]
    public int ManagerId { get; set; }

    [Column("branch_id")]
    public int BranchId { get; set; }

    [Column("supplier_id")]
    public int SupplierId { get; set; }

    [Column("import_date", TypeName = "datetime")]
    public DateTime? ImportDate { get; set; }

    [ForeignKey("BranchId")]
    [InverseProperty("KhoImportTickets")]
    public virtual DmBranch Branch { get; set; } = null!;

    [InverseProperty("Import")]
    public virtual ICollection<KhoImportDetail> KhoImportDetails { get; set; } = new List<KhoImportDetail>();

    [ForeignKey("ManagerId")]
    [InverseProperty("KhoImportTickets")]
    public virtual NsUser Manager { get; set; } = null!;

    [ForeignKey("SupplierId")]
    [InverseProperty("KhoImportTickets")]
    public virtual KhoSupplier Supplier { get; set; } = null!;
}

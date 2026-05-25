using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Models.Entities;

[Table("kho_supplier")]
[MySqlCollation("utf8mb4_unicode_ci")]
public partial class KhoSupplier
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("supplier_name")]
    [StringLength(255)]
    public string SupplierName { get; set; } = null!;

    [Column("phone")]
    [StringLength(20)]
    public string? Phone { get; set; }

    [Column("address")]
    [StringLength(255)]
    public string? Address { get; set; }

    [InverseProperty("Supplier")]
    public virtual ICollection<KhoImportTicket> KhoImportTickets { get; set; } = new List<KhoImportTicket>();

    [InverseProperty("Supplier")]
    public virtual ICollection<KhoProduct> KhoProducts { get; set; } = new List<KhoProduct>();
}

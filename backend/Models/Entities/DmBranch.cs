using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Models.Entities;

[Table("dm_branch")]
[MySqlCollation("utf8mb4_unicode_ci")]
public partial class DmBranch
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    [StringLength(255)]
    public string Name { get; set; } = null!;

    [Column("address")]
    [StringLength(255)]
    public string? Address { get; set; }

    [Column("latitude")]
    [Precision(10, 8)]
    public decimal? Latitude { get; set; }

    [Column("longitude")]
    [Precision(11, 8)]
    public decimal? Longitude { get; set; }

    [InverseProperty("Branch")]
    public virtual ICollection<CaBranchShiftConfig> CaBranchShiftConfigs { get; set; } = new List<CaBranchShiftConfig>();

    [InverseProperty("Branch")]
    public virtual ICollection<CaShift> CaShifts { get; set; } = new List<CaShift>();

    [InverseProperty("Branch")]
    public virtual ICollection<KhoBranchFrontStock> KhoBranchFrontStocks { get; set; } = new List<KhoBranchFrontStock>();

    [InverseProperty("Branch")]
    public virtual ICollection<KhoBranchInventory> KhoBranchInventories { get; set; } = new List<KhoBranchInventory>();

    [InverseProperty("Branch")]
    public virtual ICollection<KhoExportTicket> KhoExportTickets { get; set; } = new List<KhoExportTicket>();

    [InverseProperty("Branch")]
    public virtual ICollection<KhoImportTicket> KhoImportTickets { get; set; } = new List<KhoImportTicket>();

    [InverseProperty("Branch")]
    public virtual ICollection<KhoShiftClosingReport> KhoShiftClosingReports { get; set; } = new List<KhoShiftClosingReport>();

    [InverseProperty("Branch")]
    public virtual ICollection<LuongSalaryRule> LuongSalaryRules { get; set; } = new List<LuongSalaryRule>();

    [InverseProperty("Branch")]
    public virtual ICollection<NsUser> NsUsers { get; set; } = new List<NsUser>();
}

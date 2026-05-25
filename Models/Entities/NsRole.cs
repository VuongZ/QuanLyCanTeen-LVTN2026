using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Models.Entities;

[Table("ns_role")]
[Index("RoleName", Name = "role_name", IsUnique = true)]
[MySqlCollation("utf8mb4_unicode_ci")]
public partial class NsRole
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("role_name")]
    [StringLength(50)]
    public string RoleName { get; set; } = null!;

    [Column("description")]
    [StringLength(255)]
    public string? Description { get; set; }

    [Column("hourly_wage")]
    [Precision(10, 2)]
    public decimal? HourlyWage { get; set; }

    [Column("senior_wage")]
    [Precision(10, 2)]
    public decimal? SeniorWage { get; set; }

    [InverseProperty("Role")]
    public virtual ICollection<NsUser> NsUsers { get; set; } = new List<NsUser>();
}

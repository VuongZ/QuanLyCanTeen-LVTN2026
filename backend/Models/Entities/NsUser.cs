using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Models.Entities;

[Table("ns_user")]
[Index("BranchId", Name = "fk_user_branch")]
[Index("RoleId", Name = "fk_user_role")]
[Index("Username", Name = "username", IsUnique = true)]
[MySqlCollation("utf8mb4_unicode_ci")]
public partial class NsUser
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("username")]
    [StringLength(50)]
    public string Username { get; set; } = null!;

    [Column("password")]
    [StringLength(255)]
    public string Password { get; set; } = null!;

    [Column("full_name")]
    [StringLength(100)]
    public string? FullName { get; set; }

    [Column("branch_id")]
    public int? BranchId { get; set; }

    [Column("role_id")]
    public int? RoleId { get; set; }

    [Column("hire_date")]
    public DateOnly? HireDate { get; set; }

    [ForeignKey("BranchId")]
    [InverseProperty("NsUsers")]
    public virtual DmBranch? Branch { get; set; }

    [InverseProperty("User")]
    public virtual ICollection<CaFinalSchedule> CaFinalSchedules { get; set; } = new List<CaFinalSchedule>();

    [InverseProperty("User")]
    public virtual ICollection<CaStaffRegistration> CaStaffRegistrations { get; set; } = new List<CaStaffRegistration>();

    [InverseProperty("Manager")]
    public virtual ICollection<KhoExportTicket> KhoExportTickets { get; set; } = new List<KhoExportTicket>();

    [InverseProperty("Manager")]
    public virtual ICollection<KhoImportTicket> KhoImportTickets { get; set; } = new List<KhoImportTicket>();

    [InverseProperty("User")]
    public virtual ICollection<KhoShiftClosingReport> KhoShiftClosingReports { get; set; } = new List<KhoShiftClosingReport>();

    [InverseProperty("User")]
    public virtual ICollection<LuongMonthlySalary> LuongMonthlySalaries { get; set; } = new List<LuongMonthlySalary>();

    [ForeignKey("RoleId")]
    [InverseProperty("NsUsers")]
    public virtual NsRole? Role { get; set; }
}

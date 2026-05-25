using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Models.Entities;

[Table("ca_branch_shift_config")]
[Index("BranchId", Name = "fk_config_branch")]
[Index("ShiftId", Name = "fk_config_shift")]
[MySqlCollation("utf8mb4_unicode_ci")]
public partial class CaBranchShiftConfig
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("branch_id")]
    public int BranchId { get; set; }

    [Column("shift_id")]
    public int ShiftId { get; set; }

    [Column("day_of_week", TypeName = "enum('Monday','Tuesday','Wednesday','Thursday','Friday','Saturday','Sunday')")]
    public string DayOfWeek { get; set; } = null!;

    [Column("max_staff")]
    public int? MaxStaff { get; set; }

    [Column("row_version", TypeName = "timestamp")]
    public DateTime? RowVersion { get; set; }

    [ForeignKey("BranchId")]
    [InverseProperty("CaBranchShiftConfigs")]
    public virtual DmBranch Branch { get; set; } = null!;

    [ForeignKey("ShiftId")]
    [InverseProperty("CaBranchShiftConfigs")]
    public virtual CaShift Shift { get; set; } = null!;
}

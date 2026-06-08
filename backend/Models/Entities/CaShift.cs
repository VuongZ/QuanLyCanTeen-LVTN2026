using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Models.Entities;

[Table("ca_shift")]
[Index("BranchId", Name = "fk_branch_shift")]
[MySqlCollation("utf8mb4_unicode_ci")]
public partial class CaShift
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("shift_name")]
    [StringLength(50)]
    public string ShiftName { get; set; } = null!;

    [Column("start_time", TypeName = "time")]
    public TimeOnly StartTime { get; set; }

    [Column("end_time", TypeName = "time")]
    public TimeOnly EndTime { get; set; }

    [Column("branch_id")]
    public int? BranchId { get; set; }

    [Column("max_staff")]
    public int? MaxStaff { get; set; }

    [Column("is_ot")]
    public bool? IsOt { get; set; }

    [Column("row_version", TypeName = "timestamp")]
    public DateTime? RowVersion { get; set; }

    [ForeignKey("BranchId")]
    [InverseProperty("CaShifts")]
    public virtual DmBranch? Branch { get; set; }

    [InverseProperty("Shift")]
    public virtual ICollection<CaBranchShiftConfig> CaBranchShiftConfigs { get; set; } = new List<CaBranchShiftConfig>();

    [InverseProperty("Shift")]
    public virtual ICollection<CaFinalSchedule> CaFinalSchedules { get; set; } = new List<CaFinalSchedule>();

    [InverseProperty("Shift")]
    public virtual ICollection<CaStaffRegistration> CaStaffRegistrations { get; set; } = new List<CaStaffRegistration>();
}

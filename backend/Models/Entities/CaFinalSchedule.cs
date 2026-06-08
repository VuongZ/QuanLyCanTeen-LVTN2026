using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Models.Entities;

[Table("ca_final_schedule")]
[Index("ShiftId", Name = "fk_final_shift")]
[Index("UserId", Name = "fk_final_user")]
[MySqlCollation("utf8mb4_unicode_ci")]
public partial class CaFinalSchedule
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("shift_id")]
    public int ShiftId { get; set; }

    [Column("work_date")]
    public DateOnly WorkDate { get; set; }

    [Column("status", TypeName = "enum('DRAFT','PUBLISHED')")]
    public string? Status { get; set; }

    [InverseProperty("Schedule")]
    public virtual ICollection<CaAttendance> CaAttendances { get; set; } = new List<CaAttendance>();

    [ForeignKey("ShiftId")]
    [InverseProperty("CaFinalSchedules")]
    public virtual CaShift Shift { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("CaFinalSchedules")]
    public virtual NsUser User { get; set; } = null!;
}

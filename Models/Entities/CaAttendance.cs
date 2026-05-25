using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Models.Entities;

[Table("ca_attendance")]
[Index("ScheduleId", Name = "fk_att_schedule")]
[MySqlCollation("utf8mb4_unicode_ci")]
public partial class CaAttendance
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("schedule_id")]
    public int ScheduleId { get; set; }

    [Column("check_in_time", TypeName = "datetime")]
    public DateTime? CheckInTime { get; set; }

    [Column("check_out_time", TypeName = "datetime")]
    public DateTime? CheckOutTime { get; set; }

    [Column("status")]
    [StringLength(50)]
    public string? Status { get; set; }

    [ForeignKey("ScheduleId")]
    [InverseProperty("CaAttendances")]
    public virtual CaFinalSchedule Schedule { get; set; } = null!;
}

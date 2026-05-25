using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Models.Entities;

[Table("ca_schedule_period")]
[MySqlCollation("utf8mb4_unicode_ci")]
public partial class CaSchedulePeriod
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("branch_id")]
    public int BranchId { get; set; }

    [Column("start_date")]
    public DateOnly StartDate { get; set; }

    [Column("end_date")]
    public DateOnly EndDate { get; set; }

    [Column("status", TypeName = "enum('OPEN','DRAFT','PUBLISHED')")]
    public string? Status { get; set; }

    [Column("created_at", TypeName = "timestamp")]
    public DateTime? CreatedAt { get; set; }
}

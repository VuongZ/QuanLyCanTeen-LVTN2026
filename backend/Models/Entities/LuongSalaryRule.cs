using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Models.Entities;

[Table("luong_salary_rule")]
[Index("BranchId", Name = "branch_id")]
[MySqlCollation("utf8mb4_unicode_ci")]
public partial class LuongSalaryRule
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("branch_id")]
    public int BranchId { get; set; }

    [Column("bonus_threshold_days")]
    public int? BonusThresholdDays { get; set; }

    [Column("bonus_amount")]
    [Precision(15, 2)]
    public decimal? BonusAmount { get; set; }

    [Column("late_penalty")]
    [Precision(15, 2)]
    public decimal? LatePenalty { get; set; }

    [Column("absent_penalty")]
    [Precision(15, 2)]
    public decimal? AbsentPenalty { get; set; }

    [Column("weekend_multiplier")]
    public float? WeekendMultiplier { get; set; }

    [ForeignKey("BranchId")]
    [InverseProperty("LuongSalaryRules")]
    public virtual DmBranch Branch { get; set; } = null!;
}

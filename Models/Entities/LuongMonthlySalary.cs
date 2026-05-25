using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Models.Entities;

[Table("luong_monthly_salary")]
[Index("UserId", Name = "user_id")]
[MySqlCollation("utf8mb4_unicode_ci")]
public partial class LuongMonthlySalary
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("month")]
    public int Month { get; set; }

    [Column("year")]
    public int Year { get; set; }

    [Column("total_hours")]
    [Precision(10, 2)]
    public decimal TotalHours { get; set; }

    [Column("hourly_wage_at_time")]
    [Precision(10, 2)]
    public decimal HourlyWageAtTime { get; set; }

    [Column("total_salary")]
    [Precision(15, 2)]
    public decimal TotalSalary { get; set; }

    [Column("created_at", TypeName = "timestamp")]
    public DateTime? CreatedAt { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("LuongMonthlySalaries")]
    public virtual NsUser User { get; set; } = null!;
}

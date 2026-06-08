using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Models.Entities;

[Table("ca_staff_registration")]
[Index("ShiftId", Name = "fk_reg_shift")]
[Index("UserId", Name = "fk_reg_user")]
[MySqlCollation("utf8mb4_unicode_ci")]
public partial class CaStaffRegistration
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

    [Column("status")]
    [StringLength(20)]
    public string? Status { get; set; }

    [ForeignKey("ShiftId")]
    [InverseProperty("CaStaffRegistrations")]
    public virtual CaShift Shift { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("CaStaffRegistrations")]
    public virtual NsUser User { get; set; } = null!;
}

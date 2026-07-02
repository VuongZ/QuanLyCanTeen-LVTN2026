using System;
using System.Collections.Generic;

namespace LuanVanTotNghiep.backend.Models.Entities;

public partial class LuongMonthlySalary
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int Month { get; set; }

    public int Year { get; set; }

    public decimal TotalHours { get; set; }

    public decimal HourlyWageAtTime { get; set; }

    public decimal TotalSalary { get; set; }

    public decimal? TotalBonus { get; set; }

    public decimal? TotalPenalty { get; set; }

    public string? Status { get; set; }

    public DateTime? PaidAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<CaAttendance> CaAttendances { get; set; } = new List<CaAttendance>();

    public virtual NsUser User { get; set; } = null!;
}

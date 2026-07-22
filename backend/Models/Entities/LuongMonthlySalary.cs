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

    public DateTime? FinalizedAt { get; set; }

    public int? FinalizedByUserId { get; set; }

    public DateTime? AdminFinalizedAt { get; set; }

    public int? AdminFinalizedByUserId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<CaAttendance> CaAttendances { get; set; } = new List<CaAttendance>();

    public virtual ICollection<LuongSalaryAdjustmentHistory> AdjustmentHistories { get; set; } = new List<LuongSalaryAdjustmentHistory>();

    public virtual NsUser? FinalizedByUser { get; set; }

    public virtual NsUser? AdminFinalizedByUser { get; set; }

    public virtual NsUser User { get; set; } = null!;
}

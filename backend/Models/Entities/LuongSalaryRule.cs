using System;
using System.Collections.Generic;

namespace LuanVanTotNghiep.backend.Models.Entities;

public partial class LuongSalaryRule
{
    public int Id { get; set; }

    public int BranchId { get; set; }

    public int? BonusThresholdDays { get; set; }

    public decimal? BonusAmount { get; set; }

    public decimal? LatePenalty { get; set; }

    public decimal? AbsentPenalty { get; set; }

    public float? WeekendMultiplier { get; set; }

    public virtual DmBranch Branch { get; set; } = null!;
}

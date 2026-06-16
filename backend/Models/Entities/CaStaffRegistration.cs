using System;
using System.Collections.Generic;

namespace LuanVanTotNghiep.Models.Entities;

public partial class CaStaffRegistration
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int ShiftId { get; set; }

    public DateOnly WorkDate { get; set; }

    public string? Status { get; set; }

    public int? PeriodId { get; set; }

    public virtual CaSchedulePeriod? Period { get; set; }

    public virtual CaShift Shift { get; set; } = null!;

    public virtual NsUser User { get; set; } = null!;
}

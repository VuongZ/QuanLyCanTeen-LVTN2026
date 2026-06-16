using System;
using System.Collections.Generic;

namespace LuanVanTotNghiep.Models.Entities;

public partial class CaBranchShiftConfig
{
    public int Id { get; set; }

    public int ShiftId { get; set; }

    public string DayOfWeek { get; set; } = null!;

    public int? MaxStaff { get; set; }

    public DateTime? RowVersion { get; set; }

    public virtual CaShift Shift { get; set; } = null!;
}

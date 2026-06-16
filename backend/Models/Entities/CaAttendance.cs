using System;
using System.Collections.Generic;

namespace LuanVanTotNghiep.backend.Models.Entities;

public partial class CaAttendance
{
    public int Id { get; set; }

    public int ScheduleId { get; set; }

    public DateTime? CheckInTime { get; set; }

    public DateTime? CheckOutTime { get; set; }

    public string? Status { get; set; }

    public int? SalaryId { get; set; }

    public virtual LuongMonthlySalary? Salary { get; set; }

    public virtual CaFinalSchedule Schedule { get; set; } = null!;
}

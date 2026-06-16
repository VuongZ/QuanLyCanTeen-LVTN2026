using System;
using System.Collections.Generic;

namespace LuanVanTotNghiep.Models.Entities;

public partial class CaSchedulePeriod
{
    public int Id { get; set; }

    public int BranchId { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<CaStaffRegistration> CaStaffRegistrations { get; set; } = new List<CaStaffRegistration>();
}

using System;
using System.Collections.Generic;

namespace LuanVanTotNghiep.backend.Models.Entities;

public partial class NsRole
{
    public int Id { get; set; }

    public string RoleName { get; set; } = null!;

    public string? Description { get; set; }

    public decimal? HourlyWage { get; set; }

    public decimal? SeniorWage { get; set; }

    public virtual ICollection<NsUser> NsUsers { get; set; } = new List<NsUser>();
}

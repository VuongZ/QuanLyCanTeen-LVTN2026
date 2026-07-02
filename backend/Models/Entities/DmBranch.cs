using System;
using System.Collections.Generic;

namespace LuanVanTotNghiep.backend.Models.Entities;

public partial class DmBranch
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Address { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public virtual ICollection<CaShift> CaShifts { get; set; } = new List<CaShift>();

    public virtual ICollection<KhoBranchFrontStock> KhoBranchFrontStocks { get; set; } = new List<KhoBranchFrontStock>();

    public virtual ICollection<KhoBranchInventory> KhoBranchInventories { get; set; } = new List<KhoBranchInventory>();

    public virtual ICollection<KhoExportTicket> KhoExportTickets { get; set; } = new List<KhoExportTicket>();

    public virtual ICollection<KhoImportTicket> KhoImportTickets { get; set; } = new List<KhoImportTicket>();

    public virtual ICollection<KhoShiftClosingReport> KhoShiftClosingReports { get; set; } = new List<KhoShiftClosingReport>();

    public virtual ICollection<LuongSalaryRule> LuongSalaryRules { get; set; } = new List<LuongSalaryRule>();

    public virtual ICollection<NsUser> NsUsers { get; set; } = new List<NsUser>();
}

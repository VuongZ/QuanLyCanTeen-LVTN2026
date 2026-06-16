using System;
using System.Collections.Generic;

namespace LuanVanTotNghiep.Models.Entities;

public partial class NsUser
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string? FullName { get; set; }

    public int? BranchId { get; set; }

    public int? RoleId { get; set; }

    public DateOnly? HireDate { get; set; }

    public virtual DmBranch? Branch { get; set; }

    public virtual ICollection<CaFinalSchedule> CaFinalSchedules { get; set; } = new List<CaFinalSchedule>();

    public virtual ICollection<CaStaffRegistration> CaStaffRegistrations { get; set; } = new List<CaStaffRegistration>();

    public virtual ICollection<KhoExportTicket> KhoExportTickets { get; set; } = new List<KhoExportTicket>();

    public virtual ICollection<KhoImportTicket> KhoImportTickets { get; set; } = new List<KhoImportTicket>();

    public virtual ICollection<KhoShiftClosingReport> KhoShiftClosingReports { get; set; } = new List<KhoShiftClosingReport>();

    public virtual ICollection<LuongMonthlySalary> LuongMonthlySalaries { get; set; } = new List<LuongMonthlySalary>();

    public virtual NsRole? Role { get; set; }
}

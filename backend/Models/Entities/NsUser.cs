using System;
using System.Collections.Generic;

namespace LuanVanTotNghiep.backend.Models.Entities;

public partial class NsUser
{
    public int Id { get; set; }

    public string Password { get; set; } = null!;

    public string? FullName { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public int? BranchId { get; set; }

    public int? RoleId { get; set; }

    public DateOnly? HireDate { get; set; }

    public decimal SalaryCoefficient { get; set; }

    public bool SalaryCoefficientIsManual { get; set; }

    public string? ResetPasswordCode { get; set; }

    public DateTime? ResetPasswordExpiry { get; set; }

    public bool? IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual DmBranch? Branch { get; set; }

    public virtual ICollection<CaFinalSchedule> CaFinalSchedules { get; set; } = new List<CaFinalSchedule>();

    public virtual ICollection<CaStaffRegistration> CaStaffRegistrations { get; set; } = new List<CaStaffRegistration>();

    public virtual ICollection<KhoExportTicket> KhoExportTickets { get; set; } = new List<KhoExportTicket>();

    public virtual ICollection<KhoImportTicket> KhoImportTickets { get; set; } = new List<KhoImportTicket>();

    public virtual ICollection<KhoProduct> KhoProducts { get; set; } = new List<KhoProduct>();

    public virtual ICollection<KhoShiftClosingReport> KhoShiftClosingReportReviewedByNavigations { get; set; } = new List<KhoShiftClosingReport>();

    public virtual ICollection<KhoShiftClosingReport> KhoShiftClosingReportUsers { get; set; } = new List<KhoShiftClosingReport>();

    public virtual ICollection<LuongMonthlySalary> LuongMonthlySalaries { get; set; } = new List<LuongMonthlySalary>();

    public virtual ICollection<LuongMonthlySalary> FinalizedMonthlySalaries { get; set; } = new List<LuongMonthlySalary>();

    public virtual ICollection<LuongSalaryAdjustmentHistory> SalaryAdjustmentHistories { get; set; } = new List<LuongSalaryAdjustmentHistory>();

    public virtual ICollection<LuongSalaryAdjustmentHistory> CreatedSalaryAdjustmentHistories { get; set; } = new List<LuongSalaryAdjustmentHistory>();

    public virtual ICollection<LuongSalaryAdjustmentHistory> ReviewedSalaryAdjustmentHistories { get; set; } = new List<LuongSalaryAdjustmentHistory>();

    public virtual ICollection<LuongSalaryComplaint> SalaryComplaints { get; set; } = new List<LuongSalaryComplaint>();

    public virtual ICollection<LuongSalaryComplaint> ReviewedSalaryComplaints { get; set; } = new List<LuongSalaryComplaint>();

    public virtual ICollection<NsUserBankAccount> NsUserBankAccounts { get; set; } = new List<NsUserBankAccount>();

    public virtual NsRole? Role { get; set; }
}

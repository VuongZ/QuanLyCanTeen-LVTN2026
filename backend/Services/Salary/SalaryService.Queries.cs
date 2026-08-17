using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.backend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Services;

public partial class SalaryService
{
public async Task<List<SalaryDto>> GetByUserAsync(
        int userId,
        bool finalizedOnly = false)
    {
        await SynchronizeCurrentPendingSalarySnapshotsAsync(
            userId: userId);

        var query = _context.LuongMonthlySalaries
            .AsNoTracking()
            .Where(s => s.UserId == userId);

        if (finalizedOnly)
        {
            query = query.Where(s =>
                (s.Status ?? "").ToUpper() == "FINALIZED"
                || (s.Status ?? "").ToUpper() == "PAID");
        }

        return await query
            .OrderByDescending(s => s.Year)
            .ThenByDescending(s => s.Month)
            .Select(s => new SalaryDto
            {
                Id = s.Id,
                UserId = s.UserId,
                Username = s.User.Email ?? s.User.PhoneNumber,
                FullName = s.User.FullName,
                BranchId = s.User.BranchId,
                BranchName = s.User.Branch != null ? s.User.Branch.Name : null,
                EmploymentType = s.User.EmploymentType,
                BankName = s.User.NsUserBankAccounts.Select(b => b.BankName).FirstOrDefault(),
                BankAccountNumber = s.User.NsUserBankAccounts.Select(b => b.BankAccountNumber).FirstOrDefault(),
                BankAccountName = s.User.NsUserBankAccounts.Select(b => b.BankAccountName).FirstOrDefault(),
                Month = s.Month,
                Year = s.Year,
                TotalHours = s.TotalHours,
                HourlyWageAtTime = s.HourlyWageAtTime,
                TotalSalary = s.TotalSalary,
TotalBonus = s.TotalBonus ?? 0,
TotalPenalty = s.TotalPenalty ?? 0,

// Khoản đóng BHXH được liên kết với bảng lương.
BhxhContributionId =
    s.BhxhContributionId,

// Phần BHXH do nhân viên đóng.
SocialInsuranceDeduction =
    s.SocialInsuranceDeduction,

Status = s.Status,
                PaidAt = s.PaidAt,
                FinalizedAt = s.FinalizedAt,
                FinalizedByUserId = s.FinalizedByUserId,
                FinalizedByName = s.FinalizedByUser != null
                    ? s.FinalizedByUser.FullName ?? s.FinalizedByUser.Email ?? s.FinalizedByUser.PhoneNumber
                    : null,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<List<SalaryDto>> GetAllAsync()
    {
        await SynchronizeCurrentPendingSalarySnapshotsAsync();

        return await _context.LuongMonthlySalaries
            .AsNoTracking()
            .Where(s => s.User.Role == null
                || (s.User.Role.RoleName != "ADMIN" && s.User.Role.RoleName != "MANAGER"))
            .Include(s => s.User)
            .ThenInclude(u => u.Branch)
            .Include(s => s.User)
            .ThenInclude(u => u.NsUserBankAccounts)
            .OrderByDescending(s => s.Year)
            .ThenByDescending(s => s.Month)
            .ThenBy(s => s.User.FullName ?? s.User.Email ?? s.User.PhoneNumber)
            .Select(s => new SalaryDto
            {
                Id = s.Id,
                UserId = s.UserId,
                Username = s.User.Email ?? s.User.PhoneNumber,
                FullName = s.User.FullName,
                BranchId = s.User.BranchId,
                BranchName = s.User.Branch != null ? s.User.Branch.Name : null,
                EmploymentType = s.User.EmploymentType,
                BankName = s.User.NsUserBankAccounts.Select(b => b.BankName).FirstOrDefault(),
                BankAccountNumber = s.User.NsUserBankAccounts.Select(b => b.BankAccountNumber).FirstOrDefault(),
                BankAccountName = s.User.NsUserBankAccounts.Select(b => b.BankAccountName).FirstOrDefault(),
                Month = s.Month,
                Year = s.Year,
                TotalHours = s.TotalHours,
                HourlyWageAtTime = s.HourlyWageAtTime,
                TotalSalary = s.TotalSalary,
TotalBonus = s.TotalBonus ?? 0,
TotalPenalty = s.TotalPenalty ?? 0,

BhxhContributionId =
    s.BhxhContributionId,

SocialInsuranceDeduction =
    s.SocialInsuranceDeduction,

Status = s.Status,
                PaidAt = s.PaidAt,
                FinalizedAt = s.FinalizedAt,
                FinalizedByUserId = s.FinalizedByUserId,
                FinalizedByName = s.FinalizedByUser != null
                    ? s.FinalizedByUser.FullName ?? s.FinalizedByUser.Email ?? s.FinalizedByUser.PhoneNumber
                    : null,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<List<SalaryDto>> GetByBranchAsync(int branchId)
    {
        await SynchronizeCurrentPendingSalarySnapshotsAsync(
            branchId: branchId);

        return await _context.LuongMonthlySalaries
            .AsNoTracking()
            .Where(s => s.User.BranchId == branchId)
            .Include(s => s.User)
            .ThenInclude(u => u.Branch)
            .Include(s => s.User)
            .ThenInclude(u => u.NsUserBankAccounts)
            .OrderByDescending(s => s.Year)
            .ThenByDescending(s => s.Month)
            .ThenBy(s => s.User.FullName ?? s.User.Email ?? s.User.PhoneNumber)
            .Select(s => new SalaryDto
            {
                Id = s.Id,
                UserId = s.UserId,
                Username = s.User.Email ?? s.User.PhoneNumber,
                FullName = s.User.FullName,
                BranchId = s.User.BranchId,
                BranchName = s.User.Branch != null ? s.User.Branch.Name : null,
                EmploymentType = s.User.EmploymentType,
                BankName = s.User.NsUserBankAccounts.Select(b => b.BankName).FirstOrDefault(),
                BankAccountNumber = s.User.NsUserBankAccounts.Select(b => b.BankAccountNumber).FirstOrDefault(),
                BankAccountName = s.User.NsUserBankAccounts.Select(b => b.BankAccountName).FirstOrDefault(),
                Month = s.Month,
                Year = s.Year,
                TotalHours = s.TotalHours,
                HourlyWageAtTime = s.HourlyWageAtTime,
                TotalSalary = s.TotalSalary,
TotalBonus = s.TotalBonus ?? 0,
TotalPenalty = s.TotalPenalty ?? 0,

BhxhContributionId =
    s.BhxhContributionId,

SocialInsuranceDeduction =
    s.SocialInsuranceDeduction,

Status = s.Status,
                PaidAt = s.PaidAt,
                FinalizedAt = s.FinalizedAt,
                FinalizedByUserId = s.FinalizedByUserId,
                FinalizedByName = s.FinalizedByUser != null
                    ? s.FinalizedByUser.FullName ?? s.FinalizedByUser.Email ?? s.FinalizedByUser.PhoneNumber
                    : null,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<List<BranchSalarySummaryDto>> GetBranchSummariesAsync()
    {
        var summaries = await _context.LuongMonthlySalaries
            .AsNoTracking()
            .GroupBy(s => new
            {
                s.User.BranchId,
                BranchName = s.User.Branch != null ? s.User.Branch.Name : null,
                s.Month,
                s.Year
            })
            .Select(g => new BranchSalarySummaryDto
            {
                BranchId = g.Key.BranchId,
                BranchName = g.Key.BranchName,
                Month = g.Key.Month,
                Year = g.Key.Year,
                SalaryCount = g.Count(),

/*
  Tổng lương trước khi trừ BHXH.
*/
TotalSalary =
    g.Sum(s => s.TotalSalary),

/*
  Tổng phần BHXH do nhân viên đóng.
*/
TotalSocialInsuranceDeduction =
    g.Sum(s =>
        s.SocialInsuranceDeduction),

/*
  Tổng tiền thực nhận:

  TotalSalary - SocialInsuranceDeduction

  Nếu dữ liệu bất thường khiến kết quả âm,
  hệ thống lấy 0.
*/
TotalNetSalary =
    g.Sum(s =>
        s.TotalSalary >
        s.SocialInsuranceDeduction
            ? s.TotalSalary -
              s.SocialInsuranceDeduction
            : 0m),

/*
  Tổng tiền thực nhận chưa thanh toán.
*/
PendingTotal =
    g.Sum(s =>
        (s.Status ?? "").ToUpper() == "PAID"
            ? 0m
            : (
                s.TotalSalary >
                s.SocialInsuranceDeduction
                    ? s.TotalSalary -
                      s.SocialInsuranceDeduction
                    : 0m
            )),

/*
  Tổng tiền thực nhận đã thanh toán.
*/
PaidTotal =
    g.Sum(s =>
        (s.Status ?? "").ToUpper() == "PAID"
            ? (
                s.TotalSalary >
                s.SocialInsuranceDeduction
                    ? s.TotalSalary -
                      s.SocialInsuranceDeduction
                    : 0m
            )
            : 0m),

PendingCount =
    g.Count(s =>
        (s.Status ?? "").ToUpper() != "PAID"),

PaidCount =
    g.Count(s =>
        (s.Status ?? "").ToUpper() == "PAID"),

EmployeeCount =
    g.Select(s => s.UserId)
        .Distinct()
        .Count()
            })
            .OrderByDescending(s => s.Year)
            .ThenByDescending(s => s.Month)
            .ThenBy(s => s.BranchName ?? "Chua gan co so")
            .ToListAsync();

        var branchIds = summaries
            .Where(s => s.BranchId != null)
            .Select(s => s.BranchId!.Value)
            .Distinct()
            .ToList();

        var pendingComplaintCounts = await _context.LuongSalaryComplaints
            .AsNoTracking()
            .Where(complaint =>
                complaint.Status == "PENDING" &&
                complaint.User.BranchId != null &&
                branchIds.Contains(complaint.User.BranchId.Value))
            .GroupBy(complaint => new
            {
                BranchId = complaint.User.BranchId!.Value,
                complaint.Salary.Month,
                complaint.Salary.Year
            })
            .Select(group => new
            {
                group.Key.BranchId,
                group.Key.Month,
                group.Key.Year,
                Count = group.Count()
            })
            .ToDictionaryAsync(
                item => (item.BranchId, item.Month, item.Year),
                item => item.Count);

        var managers = await _context.NsUsers
            .AsNoTracking()
            .Include(u => u.Role)
            .Include(u => u.NsUserBankAccounts)
            .Where(u => u.BranchId != null && branchIds.Contains(u.BranchId.Value))
            .Where(u => u.Role != null && u.Role.RoleName.ToUpper().Contains("MANAGER"))
            .OrderBy(u => u.FullName ?? u.Email ?? u.PhoneNumber)
            .Select(u => new
            {
                u.Id,
                u.BranchId,
                u.FullName,
                u.Email,
                u.PhoneNumber,
                BankName = u.NsUserBankAccounts.Select(b => b.BankName).FirstOrDefault(),
                BankAccountNumber = u.NsUserBankAccounts.Select(b => b.BankAccountNumber).FirstOrDefault(),
                BankAccountName = u.NsUserBankAccounts.Select(b => b.BankAccountName).FirstOrDefault()
            })
            .ToListAsync();

        var managerByBranch = managers
            .Where(m => m.BranchId != null)
            .GroupBy(m => m.BranchId!.Value)
            .ToDictionary(g => g.Key, g => g.First());

        var transfers = await _context.LuongSalaryTransfers
            .AsNoTracking()
            .Include(t => t.TransferredByUser)
            .Where(t => branchIds.Contains(t.BranchId))
            .ToListAsync();

        var transferByPeriod = transfers.ToDictionary(
            t => (t.BranchId, t.Month, t.Year),
            t => t);

        foreach (var summary in summaries)
        {
            if (summary.BranchId != null && pendingComplaintCounts.TryGetValue(
                    (summary.BranchId.Value, summary.Month, summary.Year),
                    out var pendingComplaintCount))
            {
                summary.PendingComplaintCount = pendingComplaintCount;
            }

            if (summary.BranchId == null || !managerByBranch.TryGetValue(summary.BranchId.Value, out var manager))
                continue;

            summary.ManagerId = manager.Id;
            summary.ManagerName = manager.FullName;
            summary.ManagerEmail = manager.Email;
            summary.ManagerPhoneNumber = manager.PhoneNumber;
            summary.ManagerBankName = manager.BankName;
            summary.ManagerBankAccountNumber = manager.BankAccountNumber;
            summary.ManagerBankAccountName = manager.BankAccountName;

            if (transferByPeriod.TryGetValue((summary.BranchId.Value, summary.Month, summary.Year), out var transfer))
            {
                summary.TransferId = transfer.Id;
                summary.IsTransferred = true;
                summary.TransferredAmount = transfer.TotalAmount;
                summary.TransferredAt = transfer.TransferredAt;
                summary.TransferredByName = transfer.TransferredByUser.FullName
                    ?? transfer.TransferredByUser.Email
                    ?? transfer.TransferredByUser.PhoneNumber;
            }
        }

        return summaries;
    }
}

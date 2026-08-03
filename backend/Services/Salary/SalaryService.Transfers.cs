using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.backend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Services;

public partial class SalaryService
{
public async Task<BranchSalarySummaryDto?> MarkBranchTransferredAsync(
        int branchId,
        int month,
        int year,
        int adminUserId)
    {
        if (month < 1 || month > 12 || year < 2000)
            throw new InvalidOperationException("Kỳ lương không hợp lệ.");

        var existingTransfer = await _context.LuongSalaryTransfers
            .AsNoTracking()
            .AnyAsync(t => t.BranchId == branchId && t.Month == month && t.Year == year);

        if (!existingTransfer)
        {
            var salaries =
    await _context.LuongMonthlySalaries
        .Include(s => s.User)
        .Where(s =>
            s.User.BranchId == branchId &&
            s.Month == month &&
            s.Year == year)
        .ToListAsync();

            if (salaries.Count == 0)
                return null;
                /*
  Chỉ được chuyển quỹ khi tất cả bảng lương
  đã được Manager chốt.

  Việc chốt lương là lúc hệ thống liên kết
  khoản đóng BHXH.
*/
var notFinalizedSalary =
    salaries.FirstOrDefault(s =>
        !string.Equals(
            s.Status,
            "FINALIZED",
            StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(
            s.Status,
            "PAID",
            StringComparison.OrdinalIgnoreCase));

if (notFinalizedSalary != null)
{
    var employeeName =
        notFinalizedSalary.User.FullName
        ?? notFinalizedSalary.User.Email
        ?? notFinalizedSalary.User.PhoneNumber
        ?? $"ID {notFinalizedSalary.UserId}";

    throw new InvalidOperationException(
        $"Bảng lương của {employeeName} chưa được chốt. " +
        "Không thể chuyển quỹ lương.");
}
/*
  Nhân viên FULL_TIME phải được liên kết
  với khoản đóng BHXH trước khi chuyển quỹ.
*/
var missingBhxhSalary =
    salaries.FirstOrDefault(s =>
        SalaryWagePolicy.IsFullTimeEquivalent(
            s.User.EmploymentType)
        &&
        (
            !s.BhxhContributionId.HasValue ||
            s.SocialInsuranceDeduction <= 0
        ));

if (missingBhxhSalary != null)
{
    var employeeName =
        missingBhxhSalary.User.FullName
        ?? missingBhxhSalary.User.Email
        ?? missingBhxhSalary.User.PhoneNumber
        ?? $"ID {missingBhxhSalary.UserId}";

    throw new InvalidOperationException(
        $"Bảng lương của {employeeName} chưa có " +
        "khoản khấu trừ BHXH. " +
        "Vui lòng chốt lại bảng lương trước khi chuyển quỹ.");
}

            var manager = await _context.NsUsers
                .AsNoTracking()
                .Include(u => u.Role)
                .Where(u => u.BranchId == branchId && u.IsDeleted != true)
                .Where(u => u.Role != null && u.Role.RoleName.ToUpper().Contains("MANAGER"))
                .OrderBy(u => u.Id)
                .FirstOrDefaultAsync();

            if (manager == null)
                throw new InvalidOperationException("Cơ sở chưa có quản lý để nhận quỹ lương.");

            _context.LuongSalaryTransfers.Add(new LuongSalaryTransfer
            {
                BranchId = branchId,
                ManagerId = manager.Id,
                TransferredByUserId = adminUserId,
                Month = month,
                Year = year,
                SalaryCount = salaries.Count,
                /*
  Admin chuyển số tiền thực tế cần trả,
  không chuyển phần BHXH đã khấu trừ.
*/
TotalAmount =
    salaries.Sum(s =>
        Math.Max(
            0m,
            s.TotalSalary -
            s.SocialInsuranceDeduction)),
                TransferredAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
        }

        return (await GetBranchSummariesAsync())
            .FirstOrDefault(s => s.BranchId == branchId && s.Month == month && s.Year == year);
    }
}

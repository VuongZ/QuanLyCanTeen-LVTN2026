using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.backend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Services;

public partial class SalaryService
{
    public async Task<BranchSalarySummaryDto?>
        MarkBranchTransferredAsync(
            int branchId,
            int month,
            int year,
            int adminUserId)
    {
        if (month < 1 ||
            month > 12 ||
            year < 2000)
        {
            throw new InvalidOperationException(
                "Kỳ lương không hợp lệ.");
        }

        var existingTransfer =
            await _context.LuongSalaryTransfers
                .AsNoTracking()
                .AnyAsync(transfer =>
                    transfer.BranchId == branchId &&
                    transfer.Month == month &&
                    transfer.Year == year);

        if (!existingTransfer)
        {
            var salaries =
                await _context.LuongMonthlySalaries
                    .Include(salary =>
                        salary.User)
                    .Where(salary =>
                        salary.User.BranchId ==
                            branchId &&
                        salary.Month == month &&
                        salary.Year == year)
                    .ToListAsync();

            if (salaries.Count == 0)
            {
                return null;
            }

            // Chỉ được chuyển quỹ khi toàn bộ bảng lương
            // trong kỳ đã được Manager chốt.
            var notFinalizedSalary =
                salaries.FirstOrDefault(salary =>
                    !string.Equals(
                        salary.Status,
                        "FINALIZED",
                        StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(
                        salary.Status,
                        "PAID",
                        StringComparison.OrdinalIgnoreCase));

            if (notFinalizedSalary != null)
            {
                var employeeName =
                    notFinalizedSalary.User.FullName ??
                    notFinalizedSalary.User.Email ??
                    notFinalizedSalary.User.PhoneNumber ??
                    $"ID {notFinalizedSalary.UserId}";

                throw new InvalidOperationException(
                    $"Bảng lương của {employeeName} " +
                    "chưa được chốt. Không thể chuyển quỹ lương.");
            }

            // Nhân viên FULL_TIME phải được liên kết với
            // khoản đóng BHXH khi chốt lương.
            //
            // SocialInsuranceDeduction có thể bằng 0 nếu
            // lương tháng không đủ để khấu trừ. Trường hợp đó
            // vẫn được chuyển quỹ và khoản BHXH giữ DRAFT
            // để Admin nhận biết khoản cần xử lý.
            var missingBhxhSalary =
                salaries.FirstOrDefault(salary =>
                    SalaryWagePolicy
                        .IsSocialInsuranceEligible(
                            salary.User.EmploymentType) &&
                    !salary.BhxhContributionId.HasValue);

            if (missingBhxhSalary != null)
            {
                var employeeName =
                    missingBhxhSalary.User.FullName ??
                    missingBhxhSalary.User.Email ??
                    missingBhxhSalary.User.PhoneNumber ??
                    $"ID {missingBhxhSalary.UserId}";

                throw new InvalidOperationException(
                    $"Bảng lương của {employeeName} chưa có " +
                    "khoản đóng BHXH được liên kết. " +
                    "Vui lòng chốt lại bảng lương trước " +
                    "khi chuyển quỹ.");
            }

            var manager =
                await _context.NsUsers
                    .AsNoTracking()
                    .Include(user =>
                        user.Role)
                    .Where(user =>
                        user.BranchId == branchId &&
                        user.IsDeleted != true)
                    .Where(user =>
                        user.Role != null &&
                        user.Role.RoleName
                            .ToUpper()
                            .Contains("MANAGER"))
                    .OrderBy(user =>
                        user.Id)
                    .FirstOrDefaultAsync();

            if (manager == null)
            {
                throw new InvalidOperationException(
                    "Cơ sở chưa có quản lý để nhận quỹ lương.");
            }

            _context.LuongSalaryTransfers.Add(
                new LuongSalaryTransfer
                {
                    BranchId =
                        branchId,
                    ManagerId =
                        manager.Id,
                    TransferredByUserId =
                        adminUserId,
                    Month =
                        month,
                    Year =
                        year,
                    SalaryCount =
                        salaries.Count,

                    // Chuyển số tiền thực nhận.
                    // Nếu lương không đủ để trừ BHXH thì
                    // SocialInsuranceDeduction bằng 0.
                    TotalAmount =
                        salaries.Sum(salary =>
                            Math.Max(
                                0m,
                                salary.TotalSalary -
                                salary
                                    .SocialInsuranceDeduction)),
                    TransferredAt =
                        DateTime.Now
                });

            await _context.SaveChangesAsync();
        }

        return (await GetBranchSummariesAsync())
            .FirstOrDefault(summary =>
                summary.BranchId == branchId &&
                summary.Month == month &&
                summary.Year == year);
    }
}

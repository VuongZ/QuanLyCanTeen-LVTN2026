using LuanVanTotNghiep.DTOs;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Services;

public partial class SalaryService
{
    public async Task<SalaryDto?> FinalizeAsync(
        int salaryId,
        int branchId,
        int managerUserId)
    {
        var salary =
            await _context.LuongMonthlySalaries
                .Include(item => item.User)
                    .ThenInclude(user => user.Branch)
                .Include(item => item.User)
                    .ThenInclude(user =>
                        user.NsUserBankAccounts)
                .Include(item =>
                    item.FinalizedByUser)
                .FirstOrDefaultAsync(item =>
                    item.Id == salaryId &&
                    item.User.BranchId == branchId);

        if (salary == null)
        {
            return null;
        }

        var status =
            (salary.Status ?? "PENDING")
                .Trim()
                .ToUpperInvariant();

        if (status == "PAID")
        {
            throw new InvalidOperationException(
                "Bảng lương đã thanh toán.");
        }

        // Bảng lương đã chốt không được tính hoặc
        // khấu trừ BHXH lại.
        //
        // Nếu chạy lại, khoản thu hồi BHXH cũ có thể
        // bị khấu trừ nhiều lần.
        if (status == "FINALIZED")
        {
            return ToDto(salary);
        }

        if (status != "PENDING")
        {
            throw new InvalidOperationException(
                "Trạng thái bảng lương không cho phép chốt.");
        }

        var hasPendingAdjustment =
            await _context
                .LuongSalaryAdjustmentHistories
                .AnyAsync(history =>
                    history.SalaryId == salary.Id &&
                    history.Status ==
                        AdjustmentPending);

        if (hasPendingAdjustment)
        {
            throw new InvalidOperationException(
                "Còn yêu cầu thưởng/phạt " +
                "đang chờ Admin duyệt.");
        }

        var rule =
            await _context.LuongSalaryRules
                .AsNoTracking()
                .Where(item =>
                    item.BranchId ==
                        salary.User.BranchId)
                .OrderByDescending(item =>
                    item.Id)
                .FirstOrDefaultAsync();

        if (rule != null)
        {
            var adjustment =
                await BuildAdjustmentDtoAsync(
                    salary.User,
                    salary.Month,
                    salary.Year,
                    rule);

            await SetSalaryAdjustmentTotalsAsync(
                salary,
                adjustment);
        }

        // Thứ tự xử lý:
        // 1. Khấu trừ BHXH tháng hiện tại.
        // 2. Dùng phần lương còn lại để thu hồi
        //    khoản doanh nghiệp đã ứng ở các tháng trước.
        // 3. Lưu tổng khấu trừ vào bảng lương.
        await ApplySocialInsuranceDeductionAsync(
            salary);

        salary.Status =
            "FINALIZED";

        salary.FinalizedAt =
            DateTime.Now;

        salary.FinalizedByUserId =
            managerUserId;

        // Bảng lương, khoản đóng BHXH và lịch sử thu hồi
        // được lưu trong cùng một lần SaveChanges.
        await _context.SaveChangesAsync();

        if (salary.FinalizedByUserId.HasValue)
        {
            var finalizedByReference =
                _context.Entry(salary)
                    .Reference(item =>
                        item.FinalizedByUser);

            finalizedByReference.IsLoaded =
                false;

            await finalizedByReference
                .LoadAsync();
        }

        return ToDto(salary);
    }

    public async Task<SalaryDto?> MarkPaidAsync(
        int salaryId,
        int branchId)
    {
        var salary =
            await _context.LuongMonthlySalaries
                .Include(item => item.User)
                    .ThenInclude(user => user.Branch)
                .Include(item => item.User)
                    .ThenInclude(user =>
                        user.NsUserBankAccounts)
                .Include(item =>
                    item.FinalizedByUser)
                .FirstOrDefaultAsync(item =>
                    item.Id == salaryId &&
                    item.User.BranchId == branchId);

        if (salary == null)
        {
            return null;
        }

        if (!string.Equals(
                salary.Status,
                "FINALIZED",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Manager phải chốt bảng lương " +
                "trước khi xác nhận đã trả.");
        }

        var hasPendingAdjustment =
            await _context
                .LuongSalaryAdjustmentHistories
                .AnyAsync(history =>
                    history.SalaryId == salary.Id &&
                    history.Status ==
                        AdjustmentPending);

        if (hasPendingAdjustment)
        {
            throw new InvalidOperationException(
                "Còn yêu cầu thưởng/phạt " +
                "đang chờ Admin duyệt.");
        }

        var hasPendingComplaint =
            await _context.LuongSalaryComplaints
                .AnyAsync(complaint =>
                    complaint.SalaryId == salary.Id &&
                    complaint.Status == "PENDING");

        if (hasPendingComplaint)
        {
            throw new InvalidOperationException(
                "Nhân viên đang có khiếu nại lương " +
                "chưa được xử lý.");
        }

        // Manager chỉ được xác nhận đã trả lương
        // sau khi Admin đã chuyển quỹ cho đúng cơ sở
        // và kỳ lương.
        var hasTransferredSalaryFund =
            await _context.LuongSalaryTransfers
                .AsNoTracking()
                .AnyAsync(transfer =>
                    transfer.BranchId == branchId &&
                    transfer.Month == salary.Month &&
                    transfer.Year == salary.Year);

        if (!hasTransferredSalaryFund)
        {
            throw new InvalidOperationException(
                "Admin chưa xác nhận chuyển quỹ lương " +
                "cho cơ sở.");
        }

        salary.Status =
            "PAID";

        salary.PaidAt =
            DateTime.Now;

        await _context.SaveChangesAsync();

        return ToDto(salary);
    }
}
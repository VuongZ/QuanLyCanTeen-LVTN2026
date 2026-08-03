using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.backend.Models.Entities;
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
            .Include(s => s.User)
                .ThenInclude(u => u.Branch)
            .Include(s => s.User)
                .ThenInclude(u => u.NsUserBankAccounts)
            .Include(s => s.FinalizedByUser)
            .FirstOrDefaultAsync(s =>
                s.Id == salaryId &&
                s.User.BranchId == branchId);

    if (salary == null)
    {
        return null;
    }

    var status =
        (salary.Status ?? "PENDING")
            .ToUpperInvariant();

    if (status == "PAID")
    {
        throw new InvalidOperationException(
            "Bảng lương đã thanh toán.");
    }

    if (
        status != "PENDING" &&
        status != "FINALIZED"
    )
    {
        throw new InvalidOperationException(
            "Trạng thái bảng lương không cho phép chốt.");
    }

    /*
      Chỉ thực hiện lại phần tính lương và đổi trạng thái
      khi bảng lương chưa được chốt.
    */
    if (status != "FINALIZED")
    {
        // Không cho chốt nếu còn yêu cầu thưởng/phạt
        // đang chờ Admin duyệt.
        var hasPendingAdjustment =
            await _context
                .LuongSalaryAdjustmentHistories
                .AnyAsync(h =>
                    h.SalaryId == salary.Id &&
                    h.Status == AdjustmentPending);

        if (hasPendingAdjustment)
        {
            throw new InvalidOperationException(
                "Còn yêu cầu thưởng/phạt đang chờ Admin duyệt.");
        }

        // Lấy quy tắc thưởng/phạt hiện tại của cơ sở.
        var rule =
            await _context.LuongSalaryRules
                .AsNoTracking()
                .Where(r =>
                    r.BranchId ==
                    salary.User.BranchId)
                .OrderByDescending(r => r.Id)
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

        /*
          Liên kết khoản đóng BHXH.

          PART_TIME:
          - Không khấu trừ.

          FULL_TIME:
          - Phải có khoản BHXH CONFIRMED hoặc PAID
            cùng tháng và năm.
        */
        await ApplySocialInsuranceDeductionAsync(
            salary);

        salary.Status = "FINALIZED";
        salary.FinalizedAt = DateTime.Now;
        salary.FinalizedByUserId =
            managerUserId;
    }
    else
    {
        /*
          Hỗ trợ các bảng lương đã FINALIZED từ trước
          nhưng chưa có liên kết BHXH.
        */
        await ApplySocialInsuranceDeductionAsync(
            salary);
    }

    // Chỉ lưu một lần sau khi hoàn thành mọi xử lý.
    await _context.SaveChangesAsync();

    /*
      Tải lại thông tin người chốt.

      Khi vừa gán FinalizedByUserId,
      navigation FinalizedByUser có thể vẫn đang null.
    */
    if (salary.FinalizedByUserId.HasValue)
    {
        var finalizedByReference =
            _context.Entry(salary)
                .Reference(s =>
                    s.FinalizedByUser);

        finalizedByReference.IsLoaded =
            false;

        await finalizedByReference
            .LoadAsync();
    }

    return ToDto(salary);
}

    public async Task<SalaryDto?> MarkPaidAsync(int salaryId, int branchId)
    {
        var salary = await _context.LuongMonthlySalaries
            .Include(s => s.User)
            .ThenInclude(u => u.Branch)
            .Include(s => s.User)
            .ThenInclude(u => u.NsUserBankAccounts)
            .Include(s => s.FinalizedByUser)
            .FirstOrDefaultAsync(s => s.Id == salaryId && s.User.BranchId == branchId);

        if (salary == null)
            return null;

        if (!string.Equals(salary.Status, "FINALIZED", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Manager phải chốt bảng lương trước khi xác nhận đã trả.");

        var hasPendingAdjustment = await _context.LuongSalaryAdjustmentHistories
            .AnyAsync(h => h.SalaryId == salary.Id && h.Status == AdjustmentPending);
        if (hasPendingAdjustment)
            throw new InvalidOperationException("Còn yêu cầu thưởng/phạt đang chờ Admin duyệt.");

        var hasPendingComplaint = await _context.LuongSalaryComplaints
            .AnyAsync(c => c.SalaryId == salary.Id && c.Status == "PENDING");
        if (hasPendingComplaint)
            throw new InvalidOperationException("Nhân viên đang có khiếu nại lương chưa được xử lý.");

        salary.Status = "PAID";
        salary.PaidAt = DateTime.Now;
        await _context.SaveChangesAsync();

        return ToDto(salary);
    }

  
/// Chuyển Entity bảng lương thành DTO trả về cho Frontend.
}


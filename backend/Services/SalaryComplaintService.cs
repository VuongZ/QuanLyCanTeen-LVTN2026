using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Services;

public class SalaryComplaintService(AppDbContext context)
{
    private const string Pending = "PENDING";
    private const string Resolved = "RESOLVED";

    public async Task<SalaryComplaintDto> CreateAsync(
        int salaryId,
        int userId,
        CreateSalaryComplaintDto dto)
    {
        var content = dto.Content?.Trim();
        if (string.IsNullOrWhiteSpace(content))
            throw new InvalidOperationException("Vui lòng nhập nội dung khiếu nại.");
        if (content.Length > 1000)
            throw new InvalidOperationException("Nội dung khiếu nại không được vượt quá 1000 ký tự.");

        var salary = await context.LuongMonthlySalaries
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == salaryId && s.UserId == userId)
            ?? throw new KeyNotFoundException("Không tìm thấy bảng lương.");

        var salaryStatus = (salary.Status ?? "PENDING").ToUpperInvariant();
        if (salaryStatus != "PENDING")
            throw new InvalidOperationException("Chỉ có thể khiếu nại bảng lương tạm tính trước khi Admin chốt.");

        var exists = await context.LuongSalaryComplaints
            .AnyAsync(c => c.SalaryId == salaryId);
        if (exists)
            throw new InvalidOperationException("Bạn đã gửi khiếu nại cho bảng lương này.");

        var complaint = new LuongSalaryComplaint
        {
            SalaryId = salaryId,
            UserId = userId,
            Content = content,
            Status = Pending,
            CreatedAt = DateTime.Now
        };
        context.LuongSalaryComplaints.Add(complaint);
        await context.SaveChangesAsync();

        return (await GetByIdAsync(complaint.Id))!;
    }

    public async Task<List<SalaryComplaintDto>> GetByUserAsync(int userId)
    {
        return await Project(
                context.LuongSalaryComplaints
                    .AsNoTracking()
                    .Where(c => c.UserId == userId))
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<SalaryComplaintDto>> GetByBranchAsync(int branchId)
    {
        return await Project(
                context.LuongSalaryComplaints
                    .AsNoTracking()
                    .Where(c => c.User.BranchId == branchId))
            .OrderBy(c => c.Status == Pending ? 0 : 1)
            .ThenByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<SalaryComplaintDto?> ResolveAsync(
        int complaintId,
        int branchId,
        int managerUserId,
        ResolveSalaryComplaintDto dto)
    {
        var response = dto.Response?.Trim();
        if (string.IsNullOrWhiteSpace(response))
            throw new InvalidOperationException("Vui lòng nhập nội dung phản hồi.");
        if (response.Length > 1000)
            throw new InvalidOperationException("Nội dung phản hồi không được vượt quá 1000 ký tự.");

        var complaint = await context.LuongSalaryComplaints
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == complaintId && c.User.BranchId == branchId);
        if (complaint == null)
            return null;

        if (!string.Equals(complaint.Status, Pending, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Khiếu nại này đã được xử lý.");

        complaint.Status = Resolved;
        complaint.ManagerResponse = response;
        complaint.ReviewedByUserId = managerUserId;
        complaint.ReviewedAt = DateTime.Now;
        await context.SaveChangesAsync();

        return await GetByIdAsync(complaint.Id);
    }

    private async Task<SalaryComplaintDto?> GetByIdAsync(int complaintId)
    {
        return await Project(
                context.LuongSalaryComplaints
                    .AsNoTracking()
                    .Where(c => c.Id == complaintId))
            .FirstOrDefaultAsync();
    }

    private static IQueryable<SalaryComplaintDto> Project(
        IQueryable<LuongSalaryComplaint> query)
    {
        return query.Select(c => new SalaryComplaintDto
        {
            Id = c.Id,
            SalaryId = c.SalaryId,
            UserId = c.UserId,
            EmployeeName = c.User.FullName ?? c.User.Email ?? c.User.PhoneNumber,
            BranchName = c.User.Branch != null ? c.User.Branch.Name : null,
            Month = c.Salary.Month,
            Year = c.Salary.Year,
            Content = c.Content,
            Status = c.Status,
            ManagerResponse = c.ManagerResponse,
            ReviewedByUserId = c.ReviewedByUserId,
            ReviewedByName = c.ReviewedByUser != null
                ? c.ReviewedByUser.FullName ?? c.ReviewedByUser.Email ?? c.ReviewedByUser.PhoneNumber
                : null,
            CreatedAt = c.CreatedAt,
            ReviewedAt = c.ReviewedAt
        });
    }
}

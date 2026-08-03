using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.backend.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Services;

public partial class SalaryService
{
    private const string AdjustmentPending = "PENDING";
    private const string AdjustmentApproved = "APPROVED";
    private const string AdjustmentRejected = "REJECTED";

    private readonly AppDbContext _context;

    public SalaryService(AppDbContext context)
    {
        _context = context;
    }
}

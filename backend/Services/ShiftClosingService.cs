using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Repositories;

namespace LuanVanTotNghiep.Services
{
    public partial class ShiftClosingService
    {
        private const string StatusPending = "PENDING";
        private const string StatusApproved = "APPROVED";
        private const string StatusRejected = "REJECTED";



        private readonly ShiftClosingRepo _shiftClosingRepo;
        private readonly ShiftDelegationService _shiftDelegationService;

/// <summary>
/// Nhận ShiftClosingRepo thông qua
/// Dependency Injection.
/// </summary>
public ShiftClosingService(
    ShiftClosingRepo shiftClosingRepo,
    ShiftDelegationService shiftDelegationService)
{
    _shiftClosingRepo = shiftClosingRepo;
    _shiftDelegationService = shiftDelegationService;
}
        /// <summary>
/// Lấy ca làm trong ngày mà Staff
/// cần thực hiện báo cáo kết ca.
/// </summary>
    }
}

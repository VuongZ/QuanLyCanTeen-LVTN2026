using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Repositories;

namespace LuanVanTotNghiep.Services
{
    public partial class ShiftClosingService
    {
public async Task<List<ShiftClosingReportListDto>>
    GetMyReportsAsync(
        int staffId)
{
    // Kiểm tra Staff hợp lệ.
    await GetValidStaffAsync(
        staffId
    );

    // Lấy lịch sử thông qua Repository.
    return await _shiftClosingRepo
        .GetReportsByStaffIdAsync(
            staffId
        );
}

       /// <summary>
/// Lấy chi tiết một báo cáo kết ca
/// thuộc về Staff đang đăng nhập.
/// </summary>
public async Task<ShiftClosingReportDetailDto?>
    GetMyReportDetailAsync(
        int staffId,
        int reportId)
{
    // Kiểm tra Staff hợp lệ.
    await GetValidStaffAsync(
        staffId
    );

    if (reportId <= 0)
    {
        return null;
    }

    return await _shiftClosingRepo
        .GetReportDetailByStaffAsync(
            staffId,
            reportId
        );
}

       /// <summary>
/// Lấy danh sách báo cáo kết ca
/// dành cho Manager hoặc Admin.
/// </summary>
public async Task<List<ShiftClosingReportListDto>>
    GetReportsForManagementAsync(
        int? branchId)
{
    return await _shiftClosingRepo
        .GetReportsForManagementAsync(
            branchId
        );
}

       /// <summary>
/// Lấy chi tiết báo cáo kết ca
/// dành cho Manager hoặc Admin.
/// </summary>
public async Task<ShiftClosingReportDetailDto?>
    GetReportDetailForManagementAsync(
        int reportId,
        int? branchId)
{
    if (reportId <= 0)
    {
        return null;
    }

    return await _shiftClosingRepo
        .GetReportDetailForManagementAsync(
            reportId,
            branchId
        );
}

        /// <summary>
        /// Kiểm tra tài khoản có phải Staff hợp lệ
        /// và đã được gán cơ sở hay không.
        /// </summary>
    }
}


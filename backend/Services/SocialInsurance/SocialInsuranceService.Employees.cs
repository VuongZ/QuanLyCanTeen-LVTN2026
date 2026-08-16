using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Repositories;

namespace LuanVanTotNghiep.Services;

///  
/// Xử lý toàn bộ nghiệp vụ BHXH.
///
/// Service chịu trách nhiệm:
/// - Kiểm tra dữ liệu.
/// - Kiểm tra nhân viên FULL TIME.
/// - Tính tiền đóng BHXH.
/// - Chuyển trạng thái.
/// - Không xóa cứng dữ liệu.
///
/// Repository chỉ chịu trách nhiệm đọc và lưu database.
///  
public partial class SocialInsuranceService
{
    // ========================================================
    // 1. NHÂN VIÊN FULL TIME
    // ========================================================

    public async Task<
        List<SocialInsuranceFullTimeEmployeeDto>>
        GetFullTimeEmployeesAsync()
    {
        var employees =
            await _repo.GetFullTimeEmployeesAsync();

        var profiles =
            await _repo.GetAllProfilesAsync();

        // Mỗi nhân viên chỉ có một hồ sơ nên có thể
        // chuyển danh sách hồ sơ thành Dictionary theo UserId.
        var profileByUserId =
            profiles.ToDictionary(
                profile => profile.UserId);

        return employees
            .Select(user =>
            {
                profileByUserId.TryGetValue(
                    user.Id,
                    out var profile);

                return new
                    SocialInsuranceFullTimeEmployeeDto
                {
                    UserId = user.Id,
                    FullName =
                        user.FullName
                        ?? string.Empty,
                    Email =
                        user.Email
                        ?? string.Empty,
                    PhoneNumber =
                        user.PhoneNumber,
                    EmploymentType =
                        user.EmploymentType
                        ?? string.Empty,
                    HireDate =
                        user.HireDate,
                    HasSocialInsuranceProfile =
                        profile != null,
                    ProfileStatus =
                        profile?.Status
                };
            })
            .ToList();
    }
}


using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Repositories;

namespace LuanVanTotNghiep.Services;

public partial class StaffRegistrationService
{
public async Task<IEnumerable<CaStaffRegistration>>
        GetMyScheduleAsync(
            int userId,
            int periodId)
    {
        return await _repo.GetMyRegistrationsAsync(
            userId,
            periodId);
    }

    public async Task<IEnumerable<CaStaffRegistration>>
        GetRegistrationsByPeriodAsync(int periodId)
    {
        return await _repo
            .GetRegistrationsByPeriodAsync(periodId);
    }

}


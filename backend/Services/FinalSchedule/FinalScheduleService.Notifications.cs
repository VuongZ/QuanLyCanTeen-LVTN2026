using LuanVanTotNghiep.DTOs;

namespace LuanVanTotNghiep.Services;

public partial class FinalScheduleService
{
    private async Task<PublishScheduleResultDto>
        SendPublishedScheduleEmailsAsync(
            int branchId,
            DateOnly startDate,
            DateOnly endDate)
    {
        var staff = await _repo.GetActiveBranchStaffAsync(branchId);
        var branchName =
            await _repo.GetBranchNameAsync(branchId)
            ?? $"Cơ sở {branchId}";

        var result = new PublishScheduleResultDto();

        foreach (var employee in staff)
        {
            if (string.IsNullOrWhiteSpace(employee.Email))
            {
                result.EmailSkippedCount++;
                continue;
            }

            try
            {
                await _emailService.SendSchedulePublishedEmailAsync(
                    employee.Email,
                    employee.FullName,
                    branchName,
                    startDate,
                    endDate);
                result.EmailSentCount++;
            }
            catch (Exception exception)
            {
                result.EmailFailedCount++;
                _logger.LogWarning(
                    exception,
                    "Không gửi được email công bố lịch cho UserId {UserId}.",
                    employee.Id);
            }
        }

        return result;
    }
}

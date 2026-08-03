using System.Globalization;
using System.Text;
using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LuanVanTotNghiep.Services;

public partial class EmergencyReplacementService
{
    private const string PublishedStatus =
        "PUBLISHED";

    private const string LeaveApprovedStatus =
        "LEAVE_APPROVED";

    private const string AbsentStatus =
        "ABSENT";

    private const string WaitlistStatus =
        "WAITLIST";

    private const string ReplacementSelectedStatus =
        "REPLACEMENT_SELECTED";

    private const string EmergencyReplacementType =
        "EMERGENCY_REPLACEMENT";

    private const decimal DefaultReplacementMultiplier =
        1.50m;

    // Sau giờ bắt đầu 15 phút mới được ghi nhận
    // vắng không phép.
    private const int AbsenceGraceMinutes = 15;

    private readonly EmergencyReplacementRepo _repo;

    public EmergencyReplacementService(
        EmergencyReplacementRepo repo)
    {
        _repo = repo;
    }

    private static TimeZoneInfo
        GetVietnamTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(
                "SE Asia Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById(
                "Asia/Ho_Chi_Minh");
        }
    }

    private static DateTime GetVietnamNow()
    {
        var vietnamNow =
            TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                GetVietnamTimeZone());

        return DateTime.SpecifyKind(
            vietnamNow,
            DateTimeKind.Unspecified);
    }

    private static string NormalizeText(
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized =
            value.Normalize(
                NormalizationForm.FormD);

        var builder =
            new StringBuilder();

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo
                    .GetUnicodeCategory(character) !=
                UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder
            .ToString()
            .Normalize(
                NormalizationForm.FormC)
            .ToUpperInvariant();
    }

    /// <summary>
    /// Manager ghi nhận Staff nghỉ có phép.
    /// </summary>
}

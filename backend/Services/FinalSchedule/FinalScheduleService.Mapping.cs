using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Repositories;

namespace LuanVanTotNghiep.Services;

public partial class FinalScheduleService
{
private static void ApplyNormalPublishedSchedule(
        CaFinalSchedule schedule,
        int periodId,
        int? sourceRegistrationId)
    {
        schedule.PeriodId =
            periodId;

        schedule.SourceRegistrationId =
            sourceRegistrationId;

        schedule.Status =
            PublishedStatus;

        schedule.AssignmentType =
            NormalAssignment;

        schedule.PayMultiplier =
            NormalPayMultiplier;

        schedule.ReplacesScheduleId =
            null;

        schedule.AbsenceReason =
            null;

        schedule.AbsenceMarkedByUserId =
            null;

        schedule.AbsenceMarkedAt =
            null;

        schedule.AssignedByUserId =
            null;

        schedule.AssignedAt =
            null;
    }
}


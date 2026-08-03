using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Repositories;

namespace LuanVanTotNghiep.Services;

public partial class FinalScheduleService
{
    private const string PublishedStatus =
        "PUBLISHED";

    private const string DraftStatus =
        "DRAFT";

    private const string NormalAssignment =
        "NORMAL";

    private const decimal NormalPayMultiplier =
        1.00m;

    private readonly FinalScheduleRepo _repo;
    private readonly EmailService _emailService;
    private readonly ILogger<FinalScheduleService> _logger;

    public FinalScheduleService(
        FinalScheduleRepo repo,
        EmailService emailService,
        ILogger<FinalScheduleService> logger)
    {
        _repo = repo;
        _emailService = emailService;
        _logger = logger;
    }
}

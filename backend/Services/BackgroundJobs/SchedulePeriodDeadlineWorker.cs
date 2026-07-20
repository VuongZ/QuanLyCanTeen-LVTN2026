using LuanVanTotNghiep.Repositories;

namespace LuanVanTotNghiep.Services.BackgroundJobs;

public sealed class SchedulePeriodDeadlineWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SchedulePeriodDeadlineWorker> _logger;

    // Khi test để 10 giây.
    private static readonly TimeSpan CheckInterval =
        TimeSpan.FromSeconds(10);

    public SchedulePeriodDeadlineWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<SchedulePeriodDeadlineWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Worker tự động khóa đợt đăng ký đã được khởi động.");

        // Chạy ngay khi Backend khởi động.
        await CloseExpiredPeriodsAsync(stoppingToken);

        using var timer = new PeriodicTimer(CheckInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                _logger.LogInformation(
                    "Worker đang kiểm tra các đợt đăng ký quá hạn.");

                await CloseExpiredPeriodsAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation(
                "Worker tự động khóa đợt đăng ký đã dừng.");
        }
    }

    private async Task CloseExpiredPeriodsAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();

            var repo = scope.ServiceProvider
                .GetRequiredService<SchedulePeriodRepo>();

            var today = GetVietnamToday();

            var updatedCount =
                await repo.CloseExpiredOpenPeriodsAsync(
                    today,
                    cancellationToken);

            if (updatedCount > 0)
            {
                _logger.LogInformation(
                    "Đã tự động khóa {Count} đợt đăng ký vào ngày {Date}.",
                    updatedCount,
                    today);
            }
            else
            {
                _logger.LogInformation(
                    "Không có đợt đăng ký quá hạn cần khóa vào ngày {Date}.",
                    today);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Lỗi khi tự động khóa đợt đăng ký quá hạn.");
        }
    }

    private static DateOnly GetVietnamToday()
    {
        var timeZoneId = OperatingSystem.IsWindows()
            ? "SE Asia Standard Time"
            : "Asia/Ho_Chi_Minh";

        var timeZone =
            TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

        var vietnamNow = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.UtcNow,
            timeZone);

        return DateOnly.FromDateTime(vietnamNow);
    }
}
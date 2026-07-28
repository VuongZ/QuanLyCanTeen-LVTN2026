using LuanVanTotNghiep.Services;

namespace LuanVanTotNghiep.Services.BackgroundJobs;

public sealed class SchedulePeriodDeadlineWorker
    : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SchedulePeriodDeadlineWorker> _logger;

    // Khi test để 10 giây.
    // Khi chạy thật có thể đổi thành 1 giờ.
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

        try
        {
            // Kiểm tra ngay khi Backend khởi động.
            await CloseExpiredPeriodsAsync(stoppingToken);

            using var timer =
                new PeriodicTimer(CheckInterval);

            while (
                await timer.WaitForNextTickAsync(
                    stoppingToken))
            {
                _logger.LogInformation(
                    "Worker đang kiểm tra các đợt đăng ký quá hạn.");

                await CloseExpiredPeriodsAsync(
                    stoppingToken);
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation(
                "Worker tự động khóa đợt đăng ký đã dừng.");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Worker tự động khóa đợt đăng ký gặp lỗi.");
        }
    }

    private async Task CloseExpiredPeriodsAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            await using var scope =
                _scopeFactory.CreateAsyncScope();

            var service = scope.ServiceProvider
                .GetRequiredService<SchedulePeriodService>();

            var updatedCount =
                await service.CloseExpiredOpenPeriodsAsync(
                    cancellationToken);

            if (updatedCount > 0)
            {
                _logger.LogInformation(
                    "Đã tự động khóa {Count} đợt đăng ký quá hạn.",
                    updatedCount);
            }
            else
            {
                _logger.LogDebug(
                    "Không có đợt đăng ký quá hạn cần khóa.");
            }
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
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
}
namespace LuanVanTotNghiep.Services;

public class MissingCheckoutWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MissingCheckoutWorker> _logger;

    public MissingCheckoutWorker(IServiceScopeFactory scopeFactory, ILogger<MissingCheckoutWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<CheckoutRequestService>();
                var count = await service.CreateMissingCheckoutRequestsAsync(DateTime.UtcNow);
                if (count > 0) _logger.LogInformation("Created {Count} missing-checkout requests.", count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not process missing checkouts. Ensure the checkout-request migration has been applied.");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}

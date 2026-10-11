using colosseumbackend.Configuration;
using colosseumbackend.Services;
using Microsoft.Extensions.Options;

namespace colosseumbackend.BackgroundServices;

/// <summary>Re-syncs open deals from the chain on a timer, to catch anything a client never reported.</summary>
public class DealSyncHostedService : BackgroundService
{
    private readonly IServiceScopeFactory           _scopeFactory;
    private readonly DealSyncOptions                _options;
    private readonly ILogger<DealSyncHostedService> _logger;

    public DealSyncHostedService(IServiceScopeFactory scopeFactory, IOptions<DealSyncOptions> options, ILogger<DealSyncHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options      = options.Value;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_options.Interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            using var scope = _scopeFactory.CreateScope();
            try
            {
                await scope.ServiceProvider.GetRequiredService<DealSyncService>().SyncOpenDealsAsync(_options.BatchSize);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Deal sync cycle failed");
            }
        }
    }
}

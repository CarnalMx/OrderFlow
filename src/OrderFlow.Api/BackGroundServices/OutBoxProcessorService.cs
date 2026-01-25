using OrderFlow.Application.Abstractions;
using OrderFlow.Application.Outbox;

namespace OrderFlow.Api.BackgroundServices;

public class OutboxProcessorService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessorService> _logger;
    private readonly IWorkerMetrics _metrics;

    public OutboxProcessorService(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxProcessorService> logger,
        IWorkerMetrics metrics)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _metrics = metrics;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxProcessorService started");
        _metrics.MarkRunning(true);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var processor = scope.ServiceProvider.GetRequiredService<OutboxProcessor>();

                    await processor.ProcessOnceAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "OutboxProcessorService loop error");
                }

                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
        finally
        {
            _metrics.MarkRunning(false);
        }

        _logger.LogInformation("OutboxProcessorService stopped");
    }
}
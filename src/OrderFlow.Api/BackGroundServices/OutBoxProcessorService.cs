using OrderFlow.Application.Outbox;

namespace OrderFlow.Api.BackgroundServices;

public class OutboxProcessorService : BackgroundService
{
    private readonly ILogger<OutboxProcessorService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public OutboxProcessorService(IServiceScopeFactory scopeFactory, ILogger<OutboxProcessorService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxProcessorService started");

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

        _logger.LogInformation("OutboxProcessorService stopped");
    }
}

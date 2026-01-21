using OrderFlow.Application.Outbox;

namespace OrderFlow.Api.BackgroundServices;

public class OutboxProcessorService : BackgroundService
{
    private readonly ILogger<OutboxProcessorService> _logger;
    private readonly OutboxProcessor _processor;

    public OutboxProcessorService(OutboxProcessor processor, ILogger<OutboxProcessorService> logger)
    {
        _processor = processor;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxProcessorService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _processor.ProcessOnceAsync(stoppingToken);
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
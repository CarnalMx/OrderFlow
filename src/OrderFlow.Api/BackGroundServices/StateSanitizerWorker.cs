using Microsoft.EntityFrameworkCore;
using OrderFlow.Application.Abstractions;
using OrderFlow.Infrastructure.Data;

namespace OrderFlow.Api.BackGroundServices;

public class StateSanitizerWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<StateSanitizerWorker> _logger;
    private readonly IWorkerMetrics _metrics;

    public StateSanitizerWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<StateSanitizerWorker> logger,
        IWorkerMetrics metrics)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _metrics = metrics;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return RunSanitizationCycleAsync(stoppingToken);
    }

    private async Task RunSanitizationCycleAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("StateSanitizerWorker started");
        _metrics.MarkRunning(true);
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    await SanitizeOutboxMessagesAsync(dbContext, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Graceful shutdown
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during state sanitization");
                }
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
        finally
        {
            _metrics.MarkRunning(false);
            _logger.LogInformation("StateSanitizerWorker stopped");
        }
    }

    private async Task SanitizeOutboxMessagesAsync(AppDbContext dbContext, CancellationToken ct)
    {
        var invalidMessages = await dbContext.OutboxMessages
            .Where(om =>
                om.ProcessedAtUtc != null &&
                om.LockedAtUtc != null)
            .ToListAsync(ct);
        if (!invalidMessages.Any())
            return;
        foreach (var msg in invalidMessages)
        {
            msg.LockedAtUtc = null;
            msg.LockedBy = null;
            msg.LockExpireAtUtc = null;
        }
        await dbContext.SaveChangesAsync(ct);
        _logger.LogWarning(
            "Sanitized {Count} processed-but-locked outbox messages",
            invalidMessages.Count);
    }
}



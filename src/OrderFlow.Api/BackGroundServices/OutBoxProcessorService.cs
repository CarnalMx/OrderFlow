using Microsoft.EntityFrameworkCore;
using OrderFlow.Infrastructure.Data;

namespace OrderFlow.Api.BackgroundServices;

public class OutboxProcessorService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessorService> _logger;

    private const int MaxAttempts = 5;
    private const int BaseDelaySeconds = 1;
    private const int MaxDelaySeconds = 60;

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
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var now = DateTime.UtcNow;

                // Just pending messages that are due to be processed
                var pending = await db.OutboxMessages
                    .Where(m => m.ProcessedAtUtc == null)
                    .Where(m => m.NextAttemptAtUtc == null || m.NextAttemptAtUtc <= now)
                    .OrderBy(m => m.Id)
                    .Take(10)
                    .ToListAsync(stoppingToken);

                foreach (var msg in pending)
                {
                    try
                    {
                        // ✅ Here would go the actual processing according to msg.Type
                        _logger.LogInformation("Processing outbox message {Id} {Type}", msg.Id, msg.Type);

                        // Simulation: if you want to test failures, uncomment this:
                        // if (msg.Type == "OrderConfirmed") throw new Exception("Simulated failure");

                        msg.ProcessedAtUtc = DateTime.UtcNow;
                        msg.LastError = null;
                        msg.NextAttemptAtUtc = null;
                    }
                    catch (Exception ex)
                    {
                        msg.AttemptCount += 1;
                        msg.LastError = ex.Message;

                        if (msg.AttemptCount >= MaxAttempts)
                        {
                            // Dead-letter: give up after max attempts
                            msg.ProcessedAtUtc = DateTime.UtcNow;
                            msg.NextAttemptAtUtc = null;

                            _logger.LogError(ex,
                                "Outbox message {Id} failed permanently after {Attempts} attempts",
                                msg.Id, msg.AttemptCount);
                        }
                        else
                        {
                            var delaySeconds = CalculateExponentialBackoffSeconds(msg.AttemptCount);
                            msg.NextAttemptAtUtc = DateTime.UtcNow.AddSeconds(delaySeconds);

                            _logger.LogWarning(ex,
                                "Outbox message {Id} failed (attempt {Attempt}/{Max}). Retrying in {DelaySeconds}s",
                                msg.Id, msg.AttemptCount, MaxAttempts, delaySeconds);
                        }
                    }
                }

                if (pending.Count > 0)
                    await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OutboxProcessorService loop error");
            }

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }

        _logger.LogInformation("OutboxProcessorService stopped");
    }

    private static int CalculateExponentialBackoffSeconds(int attemptCount)
    {
        // attempt 1 => 1s, 2 => 2s, 3 => 4s, 4 => 8s ...
        var delay = BaseDelaySeconds * Math.Pow(2, attemptCount - 1);

        if (delay > MaxDelaySeconds)
            return MaxDelaySeconds;

        return (int)delay;
    }
}

using Microsoft.EntityFrameworkCore;
using OrderFlow.Infrastructure.Data;
using OrderFlow.Api.Outbox;

namespace OrderFlow.Api.BackgroundServices;

public class OutboxProcessorService : BackgroundService
{
    private static readonly string WorkerId = $"worker-{Environment.MachineName}-{Guid.NewGuid():N}";
    private static readonly TimeSpan LockDuration = TimeSpan.FromSeconds(30);

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
                var handlers = scope.ServiceProvider.GetServices<IOutboxHandler>();

                var now = DateTime.UtcNow;

                // 1) FETCH: mensajes elegibles
                var pending = await db.OutboxMessages
                    .Where(m => m.ProcessedAtUtc == null)
                    .Where(m => m.NextAttemptAtUtc == null || m.NextAttemptAtUtc <= now)
                    .Where(m => m.LockedAtUtc == null || m.LockExpiresAtUtc == null || m.LockExpiresAtUtc <= now)
                    .OrderBy(m => m.Id)
                    .Take(10)
                    .ToListAsync(stoppingToken);

                // 2) CLAIM: lockearlos y guardar
                if (pending.Count > 0)
                {
                    foreach (var msg in pending)
                    {
                        msg.LockedAtUtc = now;
                        msg.LockedBy = WorkerId;
                        msg.LockExpiresAtUtc = now.Add(LockDuration);
                    }

                    await db.SaveChangesAsync(stoppingToken);
                }

                // 3) PROCESS: solo si el lock es tuyo
                foreach (var msg in pending)
                {
                    if (msg.LockedBy != WorkerId)
                        continue;

                    try
                    {
                        _logger.LogInformation("Processing outbox message {Id} {Type}", msg.Id, msg.Type);

                        // ✅ éxito
                        var handler = handlers.FirstOrDefault(h => h.Type == msg.Type)
                        ?? throw new Exception($"No handler registered for outbox type '{msg.Type}'");

                        await handler.HandleAsync(msg, stoppingToken);

                        msg.ProcessedAtUtc = DateTime.UtcNow;
                        msg.LastError = null;
                        msg.NextAttemptAtUtc = null;


                        // unlock
                        msg.LockedAtUtc = null;
                        msg.LockedBy = null;
                        msg.LockExpiresAtUtc = null;
                    }
                    catch (Exception ex)
                    {
                        msg.AttemptCount += 1;
                        msg.LastError = ex.Message;

                        if (msg.AttemptCount >= MaxAttempts)
                        {
                            // dead-letter
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

                        // unlock
                        msg.LockedAtUtc = null;
                        msg.LockedBy = null;
                        msg.LockExpiresAtUtc = null;
                    }
                }

                // guardar una sola vez al final
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

using Microsoft.Extensions.Logging;
using OrderFlow.Application.Abstractions;

namespace OrderFlow.Application.Outbox;

public class OutboxProcessor
{
    private readonly IOutboxStore _store;
    private readonly IEnumerable<IOutboxHandler> _handlers;
    private readonly ILogger<OutboxProcessor> _logger;

    private readonly string _workerId = $"worker-{Environment.MachineName}-{Guid.NewGuid():N}";
    private static readonly TimeSpan LockDuration = TimeSpan.FromSeconds(30);

    private const int Take = 10;
    private const int MaxAttempts = 5;
    private const int BaseDelaySeconds = 1;
    private const int MaxDelaySeconds = 60;

    public OutboxProcessor(
        IOutboxStore store,
        IEnumerable<IOutboxHandler> handlers,
        ILogger<OutboxProcessor> logger)
    {
        _store = store;
        _handlers = handlers;
        _logger = logger;
    }

    public async Task ProcessOnceAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        var pending = await _store.GetPendingAsync(now, Take, ct);

        if (pending.Count == 0)
            return;

        await _store.ClaimAsync(pending, _workerId, now, LockDuration, ct);

        foreach (var msg in pending)
        {
            // Safety: si por alguna razon no fue claimeado por mi, no lo proceso
            if (msg.LockedBy != _workerId)
                continue;

            try
            {
                var handler = _handlers.FirstOrDefault(h => h.Type == msg.Type)
                    ?? throw new Exception($"No handler registered for outbox type '{msg.Type}'");

                await handler.HandleAsync(msg, ct);

                await _store.MarkProcessedAsync(msg, DateTime.UtcNow, ct);
            }
            catch (Exception ex)
            {
                var attempt = msg.AttemptCount + 1; // tu intento nuevo (antes de persistir)
                var deadLetter = attempt >= MaxAttempts;

                if (deadLetter)
                {
                    _logger.LogError(ex,
                        "Outbox message {Id} failed permanently after {Attempts} attempts",
                        msg.Id, attempt);

                    await _store.MarkFailedAsync(
                        msg,
                        nowUtc: DateTime.UtcNow,
                        error: ex.Message,
                        nextAttemptAtUtc: DateTime.UtcNow, // no se usa si deadLetter=true
                        deadLetter: true,
                        ct: ct);
                }
                else
                {
                    var delaySeconds = CalculateExponentialBackoffSeconds(attempt);
                    var nextAttemptAtUtc = DateTime.UtcNow.AddSeconds(delaySeconds);

                    _logger.LogWarning(ex,
                        "Outbox message {Id} failed (attempt {Attempt}/{Max}). Retrying in {DelaySeconds}s",
                        msg.Id, attempt, MaxAttempts, delaySeconds);

                    await _store.MarkFailedAsync(
                        msg,
                        nowUtc: DateTime.UtcNow,
                        error: ex.Message,
                        nextAttemptAtUtc: nextAttemptAtUtc,
                        deadLetter: false,
                        ct: ct);
                }
            }
        }
    }

    private static int CalculateExponentialBackoffSeconds(int attemptCount)
    {
        var delay = BaseDelaySeconds * Math.Pow(2, attemptCount - 1);

        if (delay > MaxDelaySeconds)
            return MaxDelaySeconds;

        return (int)delay;
    }
}

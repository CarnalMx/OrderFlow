using OrderFlow.Domain.Models;

namespace OrderFlow.Application.Abstractions;

public interface IOutboxStore
{
    Task<List<OutboxMessage>> GetPendingAsync(DateTime nowUtc, int take, CancellationToken ct);

    Task ClaimAsync(
        List<OutboxMessage> messages,
        string workerId,
        DateTime nowUtc,
        TimeSpan lockDuration,
        CancellationToken ct);

    Task MarkProcessedAsync(OutboxMessage message, DateTime nowUtc, CancellationToken ct);

    Task MarkFailedAsync(
        OutboxMessage message,
        DateTime nowUtc,
        string error,
        DateTime nextAttemptAtUtc,
        bool deadLetter,
        CancellationToken ct);
}

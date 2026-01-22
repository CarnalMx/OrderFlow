using OrderFlow.Domain.Models;

namespace OrderFlow.Application.Abstractions;

public interface IOutboxStore
{

    Task<List<OutboxMessage>> ClaimAsync(
    DateTime nowUtc,
    int take,
    string workerId,
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

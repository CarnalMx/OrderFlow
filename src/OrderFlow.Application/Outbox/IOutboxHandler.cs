using OrderFlow.Domain.Models;

namespace OrderFlow.Application.Outbox;

public interface IOutboxHandler
{
    string Type { get; }
    Task HandleAsync(OutboxMessage message, CancellationToken ct);

}
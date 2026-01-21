using OrderFlow.Domain.Models;

namespace OrderFlow.Api.Outbox;

public interface IOutboxHandler
{
    string Type { get; }
    Task HandleAsync(OutboxMessage message, CancellationToken ct);

}
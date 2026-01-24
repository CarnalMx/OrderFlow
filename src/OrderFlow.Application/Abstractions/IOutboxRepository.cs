using OrderFlow.Domain.Models;

namespace OrderFlow.Application.Abstractions;

public interface IOutboxRepository
{
    Task AddAsync(OutboxMessage message, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);

}

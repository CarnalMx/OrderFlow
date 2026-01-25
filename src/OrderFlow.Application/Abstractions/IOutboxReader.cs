using OrderFlow.Domain.Models;

namespace OrderFlow.Application.Abstractions;


public interface IOutboxReader
{
    Task<List<OutboxMessage>> GetAsync(string? status, int take, CancellationToken ct);
    Task<OutboxMessage?> GetByIdAsync(long id, CancellationToken ct);
    Task<int> CountPendingAsync(DateTime nowUtc, CancellationToken ct);

}
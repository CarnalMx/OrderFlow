using Microsoft.EntityFrameworkCore;
using OrderFlow.Application.Abstractions;
using OrderFlow.Domain.Models;
using OrderFlow.Infrastructure.Data;

namespace OrderFlow.Infrastructure.Repositories;

public class OutboxReader : IOutboxReader
{
    private readonly AppDbContext _db;

    public OutboxReader(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<OutboxMessage>> GetAsync(string? status, int take, CancellationToken ct)
    {
        var query = _db.OutboxMessages.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            status = status.Trim().ToLowerInvariant();

            query = status switch
            {
                "pending" => query.Where(m => m.ProcessedAtUtc == null),
                "processed" => query.Where(m => m.ProcessedAtUtc != null && m.AttemptCount < 5),
                "dead" => query.Where(m => m.ProcessedAtUtc != null && m.AttemptCount >= 5),
                _ => query
            };
        }

        return await query
            .OrderByDescending(m => m.Id)
            .Take(take)
            .ToListAsync(ct);
    }

    public Task<OutboxMessage?> GetByIdAsync(long id, CancellationToken ct)
    {
        return _db.OutboxMessages
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id, ct);
    }

    public Task<int> CountPendingAsync(DateTime nowUtc, CancellationToken ct)
    {
        return _db.OutboxMessages
            .Where(m => m.ProcessedAtUtc == null)
            .Where(m => m.NextAttemptAtUtc == null || m.NextAttemptAtUtc <= nowUtc)
            .Where(m => m.LockExpireAtUtc == null || m.LockExpireAtUtc <= nowUtc)
            .CountAsync(ct);
    }

}

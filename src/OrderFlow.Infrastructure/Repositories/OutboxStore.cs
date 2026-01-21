using Microsoft.EntityFrameworkCore;
using OrderFlow.Application.Abstractions;
using OrderFlow.Domain.Models;
using OrderFlow.Infrastructure.Data;

namespace OrderFlow.Infrastructure.Repositories;

public class OutboxStore : IOutboxStore
{
    private readonly AppDbContext _db;

    public OutboxStore(AppDbContext db)
    {
        _db = db;
    }

    public Task<List<OutboxMessage>> GetPendingAsync(DateTime nowUtc, int take, CancellationToken ct)
    {
        return _db.OutboxMessages
            .Where(m => m.ProcessedAtUtc == null)
            .Where(m => m.NextAttemptAtUtc == null || m.NextAttemptAtUtc <= nowUtc)
            .Where(m => m.LockedAtUtc == null || m.LockExpiresAtUtc == null || m.LockExpiresAtUtc <= nowUtc)
            .OrderBy(m => m.Id)
            .Take(take)
            .ToListAsync(ct);
    }

    public async Task ClaimAsync(
        List<OutboxMessage> messages,
        string workerId,
        DateTime nowUtc,
        TimeSpan lockDuration,
        CancellationToken ct)
    {
        foreach (var msg in messages)
        {
            msg.LockedAtUtc = nowUtc;
            msg.LockedBy = workerId;
            msg.LockExpiresAtUtc = nowUtc.Add(lockDuration);
        }

        await _db.SaveChangesAsync(ct);
    }

    public Task MarkProcessedAsync(OutboxMessage message, DateTime nowUtc, CancellationToken ct)
    {
        message.ProcessedAtUtc = nowUtc;
        message.LastError = null;
        message.NextAttemptAtUtc = null;

        message.LockedAtUtc = null;
        message.LockedBy = null;
        message.LockExpiresAtUtc = null;

        return _db.SaveChangesAsync(ct);
    }

    public Task MarkFailedAsync(
        OutboxMessage message,
        DateTime nowUtc,
        string error,
        DateTime nextAttemptAtUtc,
        bool deadLetter,
        CancellationToken ct)
    {
        message.AttemptCount += 1;
        message.LastError = error;

        if (deadLetter)
        {
            message.ProcessedAtUtc = nowUtc;
            message.NextAttemptAtUtc = null;
        }
        else
        {
            message.NextAttemptAtUtc = nextAttemptAtUtc;
        }

        message.LockedAtUtc = null;
        message.LockedBy = null;
        message.LockExpiresAtUtc = null;

        return _db.SaveChangesAsync(ct);
    }
}

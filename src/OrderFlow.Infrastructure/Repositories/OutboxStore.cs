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

    public async Task<List<OutboxMessage>> ClaimAsync(
    DateTime nowUtc,
    int take,
    string workerId,
    TimeSpan lockDuration,
    CancellationToken ct)
    {
        var lockSeconds = (int)lockDuration.TotalSeconds;

        var updateSql = @"
;WITH cte AS (
    SELECT TOP (@Take) *
    FROM dbo.OutboxMessages WITH (UPDLOCK, READPAST, ROWLOCK)
    WHERE
        ProcessedAtUtc IS NULL
        AND (NextAttemptAtUtc IS NULL OR NextAttemptAtUtc <= @NowUtc)
        AND (LockExpireAtUtc IS NULL OR LockExpireAtUtc <= @NowUtc)
    ORDER BY Id
)
UPDATE cte
SET
    LockedAtUtc = @NowUtc,
    LockedBy = @WorkerId,
    LockExpireAtUtc = DATEADD(SECOND, @LockSeconds, @NowUtc);
";

        var affected = await _db.Database.ExecuteSqlRawAsync(
            updateSql,
            new Microsoft.Data.SqlClient.SqlParameter("@Take", take),
            new Microsoft.Data.SqlClient.SqlParameter("@NowUtc", nowUtc),
            new Microsoft.Data.SqlClient.SqlParameter("@WorkerId", workerId),
            new Microsoft.Data.SqlClient.SqlParameter("@LockSeconds", lockSeconds));

        Console.WriteLine($"[ClaimAsync] affected rows = {affected}");

        // leer lo claimeado por este worker (sin comparar LockedAtUtc exacto)
        var claimed = await _db.OutboxMessages
            .Where(m => m.LockedBy == workerId)
            .Where(m => m.LockExpireAtUtc != null)
            .OrderBy(m => m.Id)
            .Take(take)
            .ToListAsync(ct);



        return claimed;
    }

    public Task MarkProcessedAsync(OutboxMessage message, DateTime nowUtc, CancellationToken ct)
    {
        _db.OutboxMessages.Update(message);

        message.ProcessedAtUtc = nowUtc;
        message.LastError = null;
        message.NextAttemptAtUtc = null;

        message.LockedAtUtc = null;
        message.LockedBy = null;
        message.LockExpireAtUtc = null;

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
        _db.OutboxMessages.Update(message);

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
        message.LockExpireAtUtc = null;

        return _db.SaveChangesAsync(ct);
    }
}

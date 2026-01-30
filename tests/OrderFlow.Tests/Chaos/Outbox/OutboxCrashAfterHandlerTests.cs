using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OrderFlow.Application.Outbox;
using OrderFlow.Domain.Models;
using OrderFlow.Infrastructure.Data;
using OrderFlow.Infrastructure.Repositories;
using OrderFlow.Tests.Integration;
using Xunit;

namespace OrderFlow.Tests.Chaos.Outbox;

// See Chaos/README.md for context and expected outcome.

[Trait("Category", "Chaos")]
public class OutboxChaosCrashAfterHandlerTests
{
    // ───────────────────────────────────────────────
    // Entity used ONLY for chaos testing
    // ───────────────────────────────────────────────
    private sealed class ProcessedEventCounter
    {
        public int Id { get; set; }
        public int Count { get; set; }
    }

    // ───────────────────────────────────────────────
    // Handler that crashes AFTER performing side-effect
    // ───────────────────────────────────────────────
    private sealed class CrashOnceCountingHandler : IOutboxHandler
    {
        private static bool _crashed;

        private readonly AppDbContext _db;

        public CrashOnceCountingHandler(AppDbContext db)
        {
            _db = db;
        }

        public string Type => "ChaosCount";

        public async Task HandleAsync(OutboxMessage message, CancellationToken ct)
        {
            var counter = await _db.Set<ProcessedEventCounter>().FirstAsync(ct);
            counter.Count += 1;

            await _db.SaveChangesAsync(ct);

            if (!_crashed)
            {
                _crashed = true;
                throw new Exception("💥 Simulated crash after side-effect");
            }
        }
    }

    [Fact]
    public async Task ProcessOnceAsync_WhenCrashAfterHandler_DoesNotDuplicateSideEffect()
    {
        // RESET DB
        using (var dbReset = TestDbFactory.CreateDbContext())
        {
            await TestDbFactory.ResetDatabaseAsync(dbReset);

            // Create table ONLY for this test (no migrations touched)
            await dbReset.Database.ExecuteSqlRawAsync(@"
                IF OBJECT_ID('dbo.ProcessedEventCounter') IS NULL
                BEGIN
                    CREATE TABLE dbo.ProcessedEventCounter (
                        Id INT IDENTITY PRIMARY KEY,
                        Count INT NOT NULL
                    )
                END
            ");

            dbReset.Add(new ProcessedEventCounter { Count = 0 });
            await dbReset.SaveChangesAsync();
        }

        // ARRANGE
        using (var dbArrange = TestDbFactory.CreateDbContext())
        {
            dbArrange.OutboxMessages.Add(new OutboxMessage
            {
                Type = "ChaosCount",
                PayloadJson = "{}",
                CreatedAtUtc = DateTime.UtcNow
            });

            await dbArrange.SaveChangesAsync();
        }

        // ACT 1 — crash after handler
        using (var dbAct1 = TestDbFactory.CreateDbContext())
        {
            var processor = new OutboxProcessor(
                store: new OutboxStore(dbAct1),
                handlers: new[] { new CrashOnceCountingHandler(dbAct1) },
                logger: NullLogger<OutboxProcessor>.Instance,
                metrics: new FakeWorkerMetrics());

            await Assert.ThrowsAsync<Exception>(() =>
                processor.ProcessOnceAsync(CancellationToken.None));
        }

        // ACT 2 — recovery run
        using (var dbAct2 = TestDbFactory.CreateDbContext())
        {
            var processor = new OutboxProcessor(
                store: new OutboxStore(dbAct2),
                handlers: new[] { new CrashOnceCountingHandler(dbAct2) },
                logger: NullLogger<OutboxProcessor>.Instance,
                metrics: new FakeWorkerMetrics());

            await processor.ProcessOnceAsync(CancellationToken.None);
        }

        // ASSERT
        using (var dbAssert = TestDbFactory.CreateDbContext())
        {
            var counter = await dbAssert.Set<ProcessedEventCounter>().FirstAsync();
            Assert.Equal(1, counter.Count);

            var msg = await dbAssert.OutboxMessages.FirstAsync();
            Assert.NotNull(msg.ProcessedAtUtc);
        }
    }
}

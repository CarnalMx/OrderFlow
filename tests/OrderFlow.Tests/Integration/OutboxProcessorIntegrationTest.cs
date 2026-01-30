using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OrderFlow.Application.Outbox;
using OrderFlow.Domain.Models;
using OrderFlow.Infrastructure.Repositories;
using Xunit;
using Xunit.Abstractions;

namespace OrderFlow.Tests.Integration;

public class OutboxProcessorIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public OutboxProcessorIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }
    private sealed class CountingHandler : IOutboxHandler
    {
        private int _count;

        public string Type => "CountMe";

        public int Count => _count;

        public Task HandleAsync(OutboxMessage message, CancellationToken ct)
        {
            Interlocked.Increment(ref _count);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task ProcessOnceAsync_WhenHandlerMissing_IncrementsAttemptAndSchedulesRetry()
    {
        // RESET
        using (var dbReset = TestDbFactory.CreateDbContext())
        {
            await TestDbFactory.ResetDatabaseAsync(dbReset);
        }

        long msgId;

        // ARRANGE (contexto 1)
        using (var dbArrange = TestDbFactory.CreateDbContext())
        {
            var msg = new OutboxMessage
            {
                Type = "UnknownType",
                PayloadJson = "{\"orderId\":123}",
                CreatedAtUtc = DateTime.UtcNow
            };

            dbArrange.OutboxMessages.Add(msg);
            await dbArrange.SaveChangesAsync();

            msgId = msg.Id;
        }

        // ACT (contexto 2)
        using (var dbAct = TestDbFactory.CreateDbContext())
        {
            var store = new OutboxStore(dbAct);

            var processor = new OutboxProcessor(
                store,
                handlers: Array.Empty<IOutboxHandler>(),
                logger: NullLogger<OutboxProcessor>.Instance,
                metrics: new FakeWorkerMetrics());

            await processor.ProcessOnceAsync(CancellationToken.None);
        }

        // ASSERT (contexto 3)
        using (var dbAssert = TestDbFactory.CreateDbContext())
        {
            var saved = await dbAssert.OutboxMessages
                .AsNoTracking()
                .FirstAsync(x => x.Id == msgId);

            Assert.Equal(1, saved.AttemptCount);
            Assert.NotNull(saved.NextAttemptAtUtc);
            Assert.Null(saved.ProcessedAtUtc);
            Assert.NotNull(saved.LastError);
        }
    }

    [Fact]
    public async Task ProcessOnceAsync_WhenFailsMaxAttempts_DeadLettersMessage()
    {
        // RESET
        using (var dbReset = TestDbFactory.CreateDbContext())
        {
            await TestDbFactory.ResetDatabaseAsync(dbReset);
        }

        long msgId;

        // ARRANGE
        using (var dbArrange = TestDbFactory.CreateDbContext())
        {
            var msg = new OutboxMessage
            {
                Type = "UnknownType",
                PayloadJson = "{\"orderId\":999}",
                CreatedAtUtc = DateTime.UtcNow
            };

            dbArrange.OutboxMessages.Add(msg);
            await dbArrange.SaveChangesAsync();

            msgId = msg.Id;
        }

        // ACT: 5 intentos
        for (int i = 0; i < 5; i++)
        {
            // Forzar elegibilidad para el intento (evitar que NextAttemptAtUtc lo bloquee)
            using (var dbPrep = TestDbFactory.CreateDbContext())
            {
                var current = await dbPrep.OutboxMessages.FindAsync(msgId);
                Assert.NotNull(current);

                current!.NextAttemptAtUtc = DateTime.UtcNow.AddSeconds(-1);
                current.LockedAtUtc = null;
                current.LockedBy = null;
                current.LockExpireAtUtc = null;

                await dbPrep.SaveChangesAsync();
            }

            using (var dbAct = TestDbFactory.CreateDbContext())
            {
                var store = new OutboxStore(dbAct);

                var processor = new OutboxProcessor(
                    store,
                    handlers: Array.Empty<IOutboxHandler>(),
                    logger: NullLogger<OutboxProcessor>.Instance,
                    metrics: new FakeWorkerMetrics());

                await processor.ProcessOnceAsync(CancellationToken.None);
            }
        }

        // ASSERT
        using (var dbAssert = TestDbFactory.CreateDbContext())
        {
            var saved = await dbAssert.OutboxMessages
                .AsNoTracking()
                .FirstAsync(x => x.Id == msgId);

            Assert.Equal(5, saved.AttemptCount);
            Assert.NotNull(saved.ProcessedAtUtc);
            Assert.Null(saved.NextAttemptAtUtc);
            Assert.NotNull(saved.LastError);
        }
    }


    [Fact]
    public async Task ProcessOnceAsync_WhenTwoProcessorsRun_OnlyOneHandlesMessage()
    {
        // RESET
        using (var dbReset = TestDbFactory.CreateDbContext())
        {
            await TestDbFactory.ResetDatabaseAsync(dbReset);
        }

        // ARRANGE
        using (var dbArrange = TestDbFactory.CreateDbContext())
        {
            dbArrange.OutboxMessages.Add(new OutboxMessage
            {
                Type = "CountMe",
                PayloadJson = "{\"orderId\":123}",
                CreatedAtUtc = DateTime.UtcNow
            });

            await dbArrange.SaveChangesAsync();
        }

        var handler = new CountingHandler();

        // ACT: dos workers con DbContexts separados
        using var db1 = TestDbFactory.CreateDbContext();
        using var db2 = TestDbFactory.CreateDbContext();

        var p1 = new OutboxProcessor(
            store: new OutboxStore(db1),
            handlers: new[] { handler },
            logger: NullLogger<OutboxProcessor>.Instance,
            metrics: new FakeWorkerMetrics());

        var p2 = new OutboxProcessor(
            store: new OutboxStore(db2),
            handlers: new[] { handler },
            logger: NullLogger<OutboxProcessor>.Instance,
            metrics: new FakeWorkerMetrics());

        await Task.WhenAll(
            p1.ProcessOnceAsync(CancellationToken.None),
            p2.ProcessOnceAsync(CancellationToken.None));

        // ASSERT
        Assert.Equal(1, handler.Count);
    }

}

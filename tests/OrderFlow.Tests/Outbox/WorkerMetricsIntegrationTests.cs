using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using OrderFlow.Application.Outbox;
using OrderFlow.Domain.Models;
using OrderFlow.Infrastructure.Repositories;
using OrderFlow.Tests.Testing;
using Xunit;

namespace OrderFlow.Tests.Outbox;

public class WorkerMetricsIntegrationTests
{
    private sealed class NoopHandler : IOutboxHandler
    {
        public string Type => "Noop";

        public Task HandleAsync(OutboxMessage message, CancellationToken ct)
            => Task.CompletedTask;
    }

    [Fact]
    public async Task ProcessOnceAsync_WhenMessageProcessed_RecordsMetrics()
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
                Type = "Noop",
                PayloadJson = "{}",
                CreatedAtUtc = DateTime.UtcNow
            };

            dbArrange.OutboxMessages.Add(msg);
            await dbArrange.SaveChangesAsync();

            msgId = msg.Id;
        }

        var metrics = new FakeWorkerMetrics();

        // ACT
        using (var dbAct = TestDbFactory.CreateDbContext())
        {
            var store = new OutboxStore(dbAct);

            var processor = new OutboxProcessor(
                store,
                handlers: new IOutboxHandler[] { new NoopHandler() },
                logger: NullLogger<OutboxProcessor>.Instance,
                metrics: metrics);

            await processor.ProcessOnceAsync(CancellationToken.None);
        }

        // ASSERT
        var snap = metrics.GetSnapshot();

        Assert.True(snap.ProcessedMessages >= 1);
        Assert.True(snap.AvgProcessedTimeMs >= 0);
    }
}

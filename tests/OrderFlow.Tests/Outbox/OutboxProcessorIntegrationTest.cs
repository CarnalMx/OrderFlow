using Microsoft.Extensions.Logging.Abstractions;
using OrderFlow.Application.Outbox;
using OrderFlow.Domain.Models;
using OrderFlow.Infrastructure.Repositories;
using OrderFlow.Tests.Testing;
using Xunit;

namespace OrderFlow.Tests.Outbox;

public class OutboxProcessorIntegrationTests
{
    [Fact]
    public async Task ProcessOnceAsync_WhenHandlerMissing_IncrementsAttemptAndSchedulesRetry()
    {
        using var db = TestDbFactory.CreateDbContext();
        await TestDbFactory.ResetDatabaseAsync(db);

        var msg = new OutboxMessage
        {
            Type = "UnknownType",
            PayloadJson = "{\"orderId\":123}",
            CreatedAtUtc = DateTime.UtcNow
        };

        db.OutboxMessages.Add(msg);
        await db.SaveChangesAsync();

        var store = new OutboxStore(db);

        var processor = new OutboxProcessor(
            store,
            handlers: Array.Empty<IOutboxHandler>(),
            logger: NullLogger<OutboxProcessor>.Instance);

        await processor.ProcessOnceAsync(CancellationToken.None);

        var saved = await db.OutboxMessages.FindAsync(msg.Id);

        Assert.NotNull(saved);
        Assert.Equal(1, saved!.AttemptCount);
        Assert.NotNull(saved.NextAttemptAtUtc);
        Assert.Null(saved.ProcessedAtUtc);
        Assert.NotNull(saved.LastError);
    }
}

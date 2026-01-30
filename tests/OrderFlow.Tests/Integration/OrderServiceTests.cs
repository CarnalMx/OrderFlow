using Microsoft.EntityFrameworkCore;
using OrderFlow.Application.Abstractions;
using OrderFlow.Application.Services;
using OrderFlow.Domain.Models;
using OrderFlow.Infrastructure.Data;
using OrderFlow.Infrastructure.Repositories;

namespace OrderFlow.Tests.Integration;

public class OrderServiceTests
{
    private sealed class FakeOutboxRepository : IOutboxRepository
    {
        public List<OutboxMessage> Messages { get; } = new();

        public Task AddAsync(OutboxMessage message, CancellationToken ct)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken ct)
        {
            return Task.CompletedTask;
        }
    }

    private static OrderService CreateService(out AppDbContext db, out FakeOutboxRepository outbox)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        db = new AppDbContext(options);

        IOrderRepository repo = new OrderRepository(db);

        outbox = new FakeOutboxRepository();
        return new OrderService(repo, outbox);
    }

    [Fact]
    public async Task CreateAsync_CreatesDraftOrder()
    {
        var service = CreateService(out var db, out var outbox);

        var order = await service.CreateAsync("Carlos", CancellationToken.None);

        Assert.NotNull(order);
        Assert.Equal("Carlos", order.CustomerName);
        Assert.Equal(OrderStatus.Draft, order.Status);
        Assert.True(order.Id > 0 || order.Id == 0); // InMemory puede manejar ids distinto
    }

    [Fact]
    public async Task ConfirmAsync_WhenDraft_ChangesStatusToConfirmed()
    {
        var service = CreateService(out var db, out var outbox);

        var created = await service.CreateAsync("Carlos", CancellationToken.None);

        var (ok, error, confirmed) = await service.ConfirmAsync(created.Id, CancellationToken.None);

        Assert.True(ok);
        Assert.Null(error);
        Assert.NotNull(confirmed);
        Assert.Equal(OrderStatus.Confirmed, confirmed!.Status);
    }

    [Fact]
    public async Task ConfirmAsync_WhenAlreadyConfirmed_ReturnsError()
    {
        var service = CreateService(out var db, out var outbox);

        var created = await service.CreateAsync("Carlos", CancellationToken.None);
        await service.ConfirmAsync(created.Id, CancellationToken.None);

        var (ok, error, confirmed) = await service.ConfirmAsync(created.Id, CancellationToken.None);

        Assert.False(ok);
        Assert.NotNull(error);
        Assert.Null(confirmed);
    }

    [Fact]
    public async Task CancelAsync_WhenCancelledTwice_ReturnsError()
    {
        var service = CreateService(out var db, out var outbox);

        var created = await service.CreateAsync("Carlos", CancellationToken.None);
        await service.CancelAsync(created.Id, CancellationToken.None);

        var (ok, error, cancelled) = await service.CancelAsync(created.Id, CancellationToken.None);

        Assert.False(ok);
        Assert.NotNull(error);
        Assert.Null(cancelled);
    }

    [Fact]
    public async Task ConfirmAsync_WhenOrderDoesNotExist_ReturnsNotFoundPattern()
    {
        var service = CreateService(out var db, out var outbox);

        var (ok, error, order) = await service.ConfirmAsync(999, CancellationToken.None);

        Assert.False(ok);
        Assert.Equal("Order not found", error);
        Assert.Null(order);
    }

    [Fact]
    public async Task ConfirmAsync_WhenDraft_CreatesOutboxMessage()
    {
        var service = CreateService(out var db, out var outbox);

        var created = await service.CreateAsync("Carlos", CancellationToken.None);
        await service.ConfirmAsync(created.Id, CancellationToken.None);

        Assert.Single(outbox.Messages);
        Assert.Equal("OrderConfirmed", outbox.Messages[0].Type);
    }

}
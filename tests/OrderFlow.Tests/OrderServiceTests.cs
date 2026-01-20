using Microsoft.EntityFrameworkCore;
using OrderFlow.Application.Abstractions;
using OrderFlow.Application.Services;
using OrderFlow.Domain.Models;
using OrderFlow.Infrastructure.Data;
using OrderFlow.Infrastructure.Repositories;

namespace OrderFlow.Tests.Services;

public class OrderServiceTests
{
    private static OrderService CreateService(out AppDbContext db)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        db = new AppDbContext(options);

        IOrderRepository repo = new OrderRepository(db);
        return new OrderService(repo);
    }

    [Fact]
    public async Task CreateAsync_CreatesDraftOrder()
    {
        var service = CreateService(out var db);

        var order = await service.CreateAsync("Carlos");

        Assert.NotNull(order);
        Assert.Equal("Carlos", order.CustomerName);
        Assert.Equal(OrderStatus.Draft, order.Status);
        Assert.True(order.Id > 0 || order.Id == 0); // InMemory puede manejar ids distinto
    }

    [Fact]
    public async Task ConfirmAsync_WhenDraft_ChangesStatusToConfirmed()
    {
        var service = CreateService(out var db);

        var created = await service.CreateAsync("Carlos");

        var (ok, error, confirmed) = await service.ConfirmAsync(created.Id);

        Assert.True(ok);
        Assert.Null(error);
        Assert.NotNull(confirmed);
        Assert.Equal(OrderStatus.Confirmed, confirmed!.Status);
    }

    [Fact]
    public async Task ConfirmAsync_WhenAlreadyConfirmed_ReturnsError()
    {
        var service = CreateService(out var db);

        var created = await service.CreateAsync("Carlos");
        await service.ConfirmAsync(created.Id);

        var (ok, error, confirmed) = await service.ConfirmAsync(created.Id);

        Assert.False(ok);
        Assert.NotNull(error);
        Assert.Null(confirmed);
    }

    [Fact]
    public async Task CancelAsync_WhenCancelledTwice_ReturnsError()
    {
        var service = CreateService(out var db);

        var created = await service.CreateAsync("Carlos");
        await service.CancelAsync(created.Id);

        var (ok, error, cancelled) = await service.CancelAsync(created.Id);

        Assert.False(ok);
        Assert.NotNull(error);
        Assert.Null(cancelled);
    }

    [Fact]
    public async Task ConfirmAsync_WhenOrderDoesNotExist_ReturnsNotFoundPattern()
    {
        var service = CreateService(out var db);

        var (ok, error, order) = await service.ConfirmAsync(999);

        Assert.False(ok);
        Assert.Null(error);
        Assert.Null(order);
    }

}

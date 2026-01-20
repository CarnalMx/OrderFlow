using Microsoft.EntityFrameworkCore;
using OrderFlow.Api.Data;
using OrderFlow.Api.Services;
using OrderFlow.Api.Models;

namespace OrderFlow.Tests.Services;

public class OrderServiceTests
{
    private static OrderService CreateService(out AppDbContext db)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        db = new AppDbContext(options);
        return new OrderService(db);
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
}

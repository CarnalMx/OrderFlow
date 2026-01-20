using OrderFlow.Application.Abstractions;
using OrderFlow.Domain.Models;

namespace OrderFlow.Application.Services;

public class OrderService
{
    private readonly IOrderRepository _orders;
    private readonly IOutboxRepository _outbox;

    public OrderService(IOrderRepository orders, IOutboxRepository outbox)
    {
        _orders = orders;
        _outbox = outbox;
    }

    public async Task<List<Order>> GetAllAsync()
    {
        return await _orders.GetAllAsync();
    }

    public async Task<Order?> GetByIdAsync(int id)
    {
        return await _orders.GetByIdAsync(id);
    }

    public async Task<Order> CreateAsync(string customerName)
    {
        var order = new Order
        {
            CustomerName = customerName.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
            Status = OrderStatus.Draft
        };

        await _orders.AddAsync(order);
        await _orders.SaveChangesAsync();

        return order;
    }

    public async Task<(bool ok, string? error, Order? order)> ConfirmAsync(int id)
    {
        var order = await _orders.GetByIdAsync(id);
        if (order is null) return (false, null, null);

        if (order.Status != OrderStatus.Draft)
            return (false, "Only Draft orders can be confirmed", null);

        order.Status = OrderStatus.Confirmed;
        await _outbox.AddAsync(new OutboxMessage
        {
            Type = "OrderConfirmed",
            PayloadJson = $"{{\"orderId\":{order.Id}}}",
            CreatedAtUtc = DateTime.UtcNow
        });
        await _orders.SaveChangesAsync();

        return (true, null, order);
    }

    public async Task<(bool ok, string? error, Order? order)> CancelAsync(int id)
    {
        var order = await _orders.GetByIdAsync(id);
        if (order is null) return (false, null, null);

        if (order.Status == OrderStatus.Cancelled)
            return (false, "Order already cancelled", null);

        order.Status = OrderStatus.Cancelled;
        await _orders.SaveChangesAsync();

        return (true, null, order);
    }
}
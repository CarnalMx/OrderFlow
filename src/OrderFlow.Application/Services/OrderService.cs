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

    public Task<List<Order>> GetAllAsync(CancellationToken ct)
    => _orders.GetAllAsync(ct);

    public Task<Order?> GetByIdAsync(int id, CancellationToken ct)
        => _orders.GetByIdAsync(id, ct);

    public async Task<Order> CreateAsync(string customerName, CancellationToken ct)
    {
        var order = new Order
        {
            CustomerName = customerName.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
            Status = OrderStatus.Draft
        };

        await _orders.AddAsync(order, ct);
        await _orders.SaveChangesAsync(ct);

        return order;
    }

    public async Task<(bool ok, string? error, Order? order)> ConfirmAsync(int id, CancellationToken ct)
    {
        var order = await _orders.GetByIdAsync(id, ct);
        if (order is null)
            return (false, "Order not found", null);

        if (order.Status != OrderStatus.Draft)
            return (false, "Only Draft orders can be confirmed", null);

        order.Status = OrderStatus.Confirmed;

        await _outbox.AddAsync(new OutboxMessage
        {
            Type = "OrderConfirmed",
            PayloadJson = $"{{\"orderId\":{order.Id}}}",
            CreatedAtUtc = DateTime.UtcNow
        }, ct);

        await _orders.SaveChangesAsync(ct);

        return (true, null, order);
    }

    public async Task<(bool ok, string? error, Order? order)> CancelAsync(int id, CancellationToken ct)
    {
        var order = await _orders.GetByIdAsync(id, ct);
        if (order is null)
            return (false, "Order not found", null);

        if (order.Status == OrderStatus.Cancelled)
            return (false, "Order already cancelled", null);

        order.Status = OrderStatus.Cancelled;
        await _orders.SaveChangesAsync(ct);

        return (true, null, order);
    }

    public async Task<(bool ok, string? error, Order? order)> AddItemAsync(
        int orderId,
        string name,
        int quantity,
        decimal unitPrice,
        CancellationToken ct)
    {
        var order = await _orders.GetByIdAsync(orderId, ct);
        if (order is null)
            return (false, "Order not found", null);

        if (order.Status != OrderStatus.Draft)
            return (false, "Only Draft orders can be edited", null);

        if (string.IsNullOrWhiteSpace(name))
            return (false, "Item name is required", null);

        if (quantity <= 0)
            return (false, "Quantity must be > 0", null);

        if (unitPrice < 0)
            return (false, "UnitPrice must be >= 0", null);

        var item = new OrderItem
        {
            OrderId = order.Id,
            Name = name.Trim(),
            Quantity = quantity,
            UnitPrice = unitPrice
        };

        await _orders.AddItemAsync(item, ct);
        await _orders.SaveChangesAsync(ct);

        var updated = await _orders.GetByIdAsync(orderId, ct);

        return (true, null, updated);
    }

}
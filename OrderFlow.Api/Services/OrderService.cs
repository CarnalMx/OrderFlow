using Microsoft.EntityFrameworkCore;
using OrderFlow.Api.Data;
using OrderFlow.Api.Models;

namespace OrderFlow.Api.Services;

public class OrderService
{
    private readonly AppDbContext _db;

    public OrderService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Order>> GetAllAsync()
    {
        return await _db.Orders
            .OrderByDescending(o => o.Id)
            .ToListAsync();
    }

    public async Task<Order?> GetByIdAsync(int id)
    {
        return await _db.Orders.FindAsync(id);
    }

    public async Task<Order> CreateAsync(string customerName)
    {
        var order = new Order
        {
            CustomerName = customerName.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
            Status = OrderStatus.Draft
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        return order;
    }

    public async Task<(bool ok, string? error, Order? order)> ConfirmAsync(int id)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order is null) return (false, "Order not found", null);

        if (order.Status != OrderStatus.Draft)
            return (false, "Only Draft orders can be confirmed", null);

        order.Status = OrderStatus.Confirmed;
        await _db.SaveChangesAsync();

        return (true, null, order);
    }

    public async Task<(bool ok, string? error, Order? order)> CancelAsync(int id)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order is null) return (false, "Order not found", null);

        if (order.Status == OrderStatus.Cancelled)
            return (false, "Order already cancelled", null);

        order.Status = OrderStatus.Cancelled;
        await _db.SaveChangesAsync();

        return (true, null, order);
    }
}

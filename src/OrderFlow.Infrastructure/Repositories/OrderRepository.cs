using Microsoft.EntityFrameworkCore;
using OrderFlow.Application.Abstractions;
using OrderFlow.Domain.Models;
using OrderFlow.Infrastructure.Data;

namespace OrderFlow.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _db;

    public OrderRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Order>> GetAllAsync(CancellationToken ct)
    {
        return await _db.Orders
            .OrderByDescending(o => o.Id)
            .ToListAsync();
    }

    public async Task<Order?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, ct);
    }

    public Task<Order> AddAsync(Order order, CancellationToken ct)
    {
        _db.Orders.Add(order);
        return Task.FromResult(order);
    }

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        await _db.SaveChangesAsync(ct);
    }

    public Task AddItemAsync(OrderItem item, CancellationToken ct)
    {
        _db.OrderItems.Add(item);
        return Task.CompletedTask;
    }

}

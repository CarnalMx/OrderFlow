using OrderFlow.Domain.Models;

namespace OrderFlow.Application.Abstractions;

public interface IOrderRepository
{
    Task<List<Order>> GetAllAsync(CancellationToken ct);
    Task<Order?> GetByIdAsync(int id, CancellationToken ct);
    Task<Order> AddAsync(Order order, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
    Task AddItemAsync(OrderItem item, CancellationToken ct);

}
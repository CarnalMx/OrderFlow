using OrderFlow.Domain.Models;

namespace OrderFlow.Application.Abstractions;

public interface IOrderRepository
{
    Task<List<Order>> GetAllAsync();
    Task<Order?> GetByIdAsync(int id);
    Task<Order> AddAsync(Order order);
    Task SaveChangesAsync();
}
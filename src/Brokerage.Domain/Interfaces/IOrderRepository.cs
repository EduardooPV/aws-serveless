using Brokerage.Domain.Entities;

namespace Brokerage.Domain.Interfaces;

public interface IOrderRepository
{
    Task CreateAsync(Order order);

    Task<Order?> GetByIdAsync(Guid id);
}
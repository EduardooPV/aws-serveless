using Brokerage.Domain.Entities;

namespace Brokerage.Domain.Interfaces;

public interface IOrderRepository
{
    Task SaveAsync(Order order, CancellationToken cancellationToken = default);

    Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task<bool> UpdateStatusAsync(Guid orderId, string status, string expectedStatus, CancellationToken cancellationToken = default);

    Task<IEnumerable<Order>?> GetAllOrdersAsync(CancellationToken cancellationToken = default);
}
using Brokerage.Domain.Entities;

namespace Brokerage.Domain.Interfaces;

public interface IOrderQueue
{
    Task EnqueueAsync(Order order);
}

using System;
using Brokerage.Domain.Entities;

namespace Brokerage.Domain.Interfaces;

public interface IOrderOrchestrator
{
    Task StartOrderProcessingSagaAsync(Order order, CancellationToken cancellationToken = default);
}

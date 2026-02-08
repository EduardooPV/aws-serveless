using System;
using System.Text.Json;
using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;
using Brokerage.Domain.Entities;
using Brokerage.Domain.Interfaces;

namespace Brokerage.Infrastructure.Functions;

public class OrderStepFunctionsOrchestrator(IAmazonStepFunctions sfn) : IOrderOrchestrator
{
    private const string StateMachineArn = "arn:aws:states:us-east-1:000000000000:stateMachine:OrderProcessorSaga";

    public async Task StartOrderProcessingSagaAsync(Order order, CancellationToken cancellationToken = default)
    {
        var input = new
        {
            OrderId = order.Id,
            CustomerId = order.CustomerId,
            TotalAmount = order.Quantity * order.Price,
            StockSymbol = order.StockSymbol
        };

        var request = new StartExecutionRequest
        {
            StateMachineArn = StateMachineArn,
            Input = JsonSerializer.Serialize(input),
            Name = $"Order-{order.Id}-{Guid.NewGuid().ToString()[..8]}"
        };

        await sfn.StartExecutionAsync(request);
    }
}

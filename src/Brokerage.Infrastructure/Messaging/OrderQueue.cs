using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Brokerage.Domain.Entities;
using Brokerage.Domain.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Brokerage.Infrastructure.Messaging;

public class OrderQueue(IAmazonSQS sqs, IConfiguration configuration) : IOrderQueue
{
    private readonly IAmazonSQS _sqs = sqs;
    private readonly string _queueUrl = configuration["AWS:SQS:OrderQueueUrl"]
            ?? throw new Exception("Queue URL não configurada");

    public async Task EnqueueAsync(Order order)
    {
        var message = JsonSerializer.Serialize(new
        {
            order.Id,
            order.CustomerId,
            order.StockSymbol,
            order.Quantity,
            order.Price,
            order.Status,
            order.CreatedAt
        });

        await _sqs.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = _queueUrl,
            MessageBody = message
        });
    }
}

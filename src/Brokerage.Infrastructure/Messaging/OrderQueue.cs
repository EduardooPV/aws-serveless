using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Brokerage.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Brokerage.Domain.Messages;

namespace Brokerage.Infrastructure.Messaging;

public class OrderQueue(IAmazonSQS sqs, IConfiguration configuration) : IOrderQueue
{
    private readonly IAmazonSQS _sqs = sqs;
    private readonly string _queueUrl = configuration["AWS:SQS:OrderQueueUrl"]
            ?? throw new Exception("Queue URL não configurada");

    public async Task PublishOrderCreatedAsync(string orderId)
    {
        var message = new OrderCreatedMessage
        {
            OrderId = orderId
        };


        await _sqs.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = _queueUrl,
            MessageBody = JsonSerializer.Serialize(message)
        });
    }
}

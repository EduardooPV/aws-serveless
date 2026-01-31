using Amazon.SQS;
using Amazon.SQS.Model;
using Brokerage.Domain.Messages;
using System.Text.Json;

namespace Brokerage.Worker;

public sealed class OrderProcessorWorker(IAmazonSQS sqs, ILogger<OrderProcessorWorker> logger) : BackgroundService
{
    private readonly IAmazonSQS _sqs = sqs;
    private readonly ILogger<OrderProcessorWorker> _logger = logger;
    private const string QueueUrl = "http://localhost:4566/000000000000/OrderQueue";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Order Worker Started at: {time}", DateTimeOffset.Now);

        while (!stoppingToken.IsCancellationRequested)
        {
            var response = await _sqs.ReceiveMessageAsync(new ReceiveMessageRequest
            {
                QueueUrl = QueueUrl,
                MaxNumberOfMessages = 1,
                WaitTimeSeconds = 10
            }, stoppingToken);

            foreach (var message in response.Messages)
            {
                await ProcessMessageAsync(message);
                await _sqs.DeleteMessageAsync(QueueUrl, message.ReceiptHandle);
            }
        }
    }

    private async Task ProcessMessageAsync(Message message)
    {
        var payload = JsonSerializer.Deserialize<OrderCreatedMessage>(message.Body);

        if (payload is null)
        {
            _logger.LogWarning("Invalid message payload: {Body}", message.Body);
            return;
        }

        _logger.LogInformation("Processing order {OrderId}", payload!.OrderId);

        await Task.Delay(500);
    }
}
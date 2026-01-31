using Amazon.SQS;
using Amazon.SQS.Model;
using Brokerage.Domain.Entities;
using Brokerage.Domain.Interfaces;
using Brokerage.Domain.Messages;
using System.Text.Json;

namespace Brokerage.Worker;

public sealed class OrderProcessorWorker(IAmazonSQS sqs, ILogger<OrderProcessorWorker> logger, IServiceScopeFactory scopeFactory) : BackgroundService
{
    private readonly IAmazonSQS _sqs = sqs;
    private readonly ILogger<OrderProcessorWorker> _logger = logger;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
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

            if (response.Messages is null || response.Messages.Count == 0)
            {
                continue;
            }

            foreach (var message in response.Messages)
                try
                {
                    await ProcessMessageAsync(message, stoppingToken);

                    await _sqs.DeleteMessageAsync(QueueUrl, message.ReceiptHandle, stoppingToken);
                }
                catch (Exception exception)
                {
                    _logger.LogError(
                        exception,
                        "Error processing message {MessageId}",
                        message.MessageId
                    );
                }
        }
    }

    private async Task ProcessMessageAsync(Message message, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();

        var payload = JsonSerializer.Deserialize<OrderCreatedMessage>(message.Body);

        if (payload is null)
        {
            _logger.LogWarning("Invalid message payload: {Body}", message.Body);
            return;
        }

        var orderId = Guid.Parse(payload.OrderId);

        _logger.LogInformation("Order {OrderId} received. Updating status to Processing", orderId);

        await orderRepository.UpdateStatusAsync(orderId, OrderStatuses.Processing, cancellationToken);
    }
}
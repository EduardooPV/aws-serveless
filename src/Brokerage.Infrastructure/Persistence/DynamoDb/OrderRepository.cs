using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Brokerage.Domain.Entities;
using Brokerage.Domain.Interfaces;

namespace Brokerage.Infrastructure.Persistence.DynamoDb;

public class OrderRepository(IAmazonDynamoDB dynamoDb) : IOrderRepository
{
    private const string TableName = "Orders";

    private readonly IAmazonDynamoDB _dynamoDb = dynamoDb;

    public async Task SaveAsync(Order order, CancellationToken cancellationToken = default)
    {
        var request = new PutItemRequest
        {
            TableName = TableName,
            Item = new Dictionary<string, AttributeValue>
            {
                ["order_id"] = new AttributeValue { S = order.Id.ToString() },
                ["customer_id"] = new AttributeValue { S = order.CustomerId },
                ["stock_symbol"] = new AttributeValue { S = order.StockSymbol },
                ["quantity"] = new AttributeValue { N = order.Quantity.ToString() },
                ["price"] = new AttributeValue { N = order.Price.ToString() },
                ["status"] = new AttributeValue { S = order.Status },
                ["created_at"] = new AttributeValue { S = order.CreatedAt.ToString("O") }
            }

        };

        await _dynamoDb.PutItemAsync(request, cancellationToken);
    }

    public async Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var response = await _dynamoDb.GetItemAsync(new GetItemRequest
        {
            TableName = TableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["order_id"] = new AttributeValue { S = orderId.ToString() }
            }
        }, cancellationToken);

        if (response.Item is null || response.Item.Count == 0)
            return null;

        return new Order(
            id: orderId,
            customerId: response.Item["customer_id"].S,
            stockSymbol: response.Item["stock_symbol"].S,
            quantity: int.Parse(response.Item["quantity"].N),
            price: decimal.Parse(response.Item["price"].N),
            status: response.Item["status"].S,
            createdAt: DateTime.Parse(response.Item["created_at"].S)
        );
    }

    public async Task<bool> UpdateStatusAsync(Guid orderId, string status, string expectedStatus, CancellationToken cancellationToken = default)
    {
        var request = new UpdateItemRequest
        {
            TableName = TableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["order_id"] = new AttributeValue { S = orderId.ToString() }
            },
            UpdateExpression = "SET #status = :status",
            ConditionExpression = "#status = :expectedStatus",
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                ["#status"] = "status"
            },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":status"] = new AttributeValue { S = status },
                [":expectedStatus"] = new AttributeValue { S = expectedStatus }
            }
        };

        try
        {
            await _dynamoDb.UpdateItemAsync(request, cancellationToken);
            return true;
        }
        catch (ConditionalCheckFailedException)
        {
            Console.WriteLine($"Ordem {orderId} já não está mais no estado {expectedStatus}. Ignorando.");
            return false;
        }
    }

    public async Task<IEnumerable<Order>?> GetAllOrdersAsync(CancellationToken cancellationToken = default)
    {
        var response = await _dynamoDb.ScanAsync(new ScanRequest
        {
            TableName = TableName
        }, cancellationToken);

        if (response.Items is null || response.Items.Count == 0)
        {
            return null;
        }

        return response.Items.Select(item => new Order(
            Guid.Parse(item["order_id"].S),
            customerId: item["customer_id"].S,
            stockSymbol: item["stock_symbol"].S,
            quantity: int.Parse(item["quantity"].N),
            price: decimal.Parse(item["price"].N),
            status: item["status"].S,
            createdAt: DateTime.Parse(item["created_at"].S)
        )).ToList();
    }
}

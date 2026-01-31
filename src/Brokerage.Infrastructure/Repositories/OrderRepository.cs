using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Brokerage.Domain.Entities;
using Brokerage.Domain.Interfaces;

namespace Brokerage.Infrastructure.Repositories;

public class OrderRepository(IAmazonDynamoDB dynamoDb) : IOrderRepository
{
    private readonly IAmazonDynamoDB _dynamoDb = dynamoDb;


    public async Task CreateAsync(Order order)
    {
        var request = new PutItemRequest

        {
            TableName = "Orders",
            Item = new Dictionary<string, AttributeValue>
            {
                { "order_id", new AttributeValue { S = order.Id.ToString() } },
                { "customer_id", new AttributeValue { S = order.CustomerId } },
                { "stock_symbol", new AttributeValue { S = order.StockSymbol } },
                { "status", new AttributeValue { S = order.Status } },
                { "created_at", new AttributeValue { S = order.CreatedAt.ToString("O") } },
                { "quantity", new AttributeValue { N = order.Quantity.ToString() } },
                {
                    "price",
                    new AttributeValue { N = order.Price.ToString(System.Globalization.CultureInfo.InvariantCulture) }
                },

                {
                    "total_amount",
                    new AttributeValue
                        { N = order.TotalAmount.ToString(System.Globalization.CultureInfo.InvariantCulture) }
                }
            }
        };

        await _dynamoDb.PutItemAsync(request);
    }

    public async Task<Order?> GetByIdAsync(Guid id)
    {
        var request = new GetItemRequest
        {
            TableName = "Orders",
            Key = new Dictionary<string, AttributeValue>
            {
                { "order_id", new AttributeValue { S = id.ToString() } }
            }
        };

        var response = await _dynamoDb.GetItemAsync(request);

        if (response.Item == null || response.Item.Count == 0)
        {
            return null;
        }

        var item = response.Item;

        var customerId = item["customer_id"].S;
        var stockSymbol = item["stock_symbol"].S;
        var status = item["status"].S;
        var createdAt = item["created_at"].S;
        var quantity = item["quantity"].N;
        var price = item["price"].N;

        var priceDecimal = decimal.Parse(price, System.Globalization.CultureInfo.InvariantCulture);
        var quantityInt = int.Parse(quantity);
        var createdAtDate = DateTime.Parse(createdAt);
        var orderIdGuid = Guid.Parse(item["order_id"].S);

        return new Order(orderIdGuid, customerId, stockSymbol, quantityInt, priceDecimal, status, createdAtDate);
    }
}
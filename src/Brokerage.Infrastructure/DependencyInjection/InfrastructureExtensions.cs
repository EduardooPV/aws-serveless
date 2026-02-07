using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Amazon.SQS;
using Microsoft.Extensions.DependencyInjection;
using Brokerage.Domain.Interfaces;
using Brokerage.Infrastructure.Persistence.DynamoDb;
using Brokerage.Infrastructure.Messaging;
using Amazon.SimpleNotificationService;

namespace Brokerage.Infrastructure.DependencyInjection;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services)
    {
        var credentials = new BasicAWSCredentials("test", "test");

        var dynamoConfig = new AmazonDynamoDBConfig
        {
            ServiceURL = "http://localhost:4566",
            AuthenticationRegion = "us-east-1"
        };
        var sqsConfig = new AmazonSQSConfig
        {
            ServiceURL = "http://localhost:4566",
            AuthenticationRegion = "us-east-1",
            UseHttp = true
        };
        var snsConfig = new AmazonSimpleNotificationServiceConfig
        {
            ServiceURL = "http://localhost:4566",
            AuthenticationRegion = "us-east-1"
        };

        services.AddSingleton<IAmazonDynamoDB>(
            new AmazonDynamoDBClient(credentials, dynamoConfig)
        );
        services.AddSingleton<IAmazonSQS>(
            new AmazonSQSClient(credentials, sqsConfig)
        );
        services.AddSingleton<IAmazonSimpleNotificationService>(
            new AmazonSimpleNotificationServiceClient(credentials, snsConfig)
        );

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderQueue, OrderQueue>();

        return services;
    }
}

using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Amazon.SQS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Brokerage.Domain.Interfaces;
using Brokerage.Infrastructure.Repositories;
using Brokerage.Infrastructure.Messaging;

namespace Brokerage.Infrastructure.DependencyInjection;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
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

        services.AddSingleton<IAmazonDynamoDB>(
            new AmazonDynamoDBClient(credentials, dynamoConfig)
        );
        services.AddSingleton<IAmazonSQS>(
            new AmazonSQSClient(credentials, sqsConfig)
        );

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderQueue, OrderQueue>();

        return services;
    }
}

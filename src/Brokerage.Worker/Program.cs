using Amazon.SQS;
using Brokerage.Domain.Interfaces;
using Brokerage.Infrastructure.DependencyInjection;
using Brokerage.Infrastructure.Persistence.DynamoDb;
using Brokerage.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInfrastructure();

builder.Services.AddScoped<IOrderRepository, OrderRepository>();

builder.Services.AddHostedService<OrderProcessorWorker>();

var host = builder.Build();
host.Run();

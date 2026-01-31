using Amazon.SQS;
using Brokerage.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<IAmazonSQS>(_ =>
{
    return new AmazonSQSClient(new AmazonSQSConfig
    {
        ServiceURL = "http://localhost:4566"
    });
});

builder.Services.AddHostedService<OrderProcessorWorker>();

var host = builder.Build();
host.Run();

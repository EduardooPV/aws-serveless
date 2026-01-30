using System.Reflection;
using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Brokerage.Domain.Interfaces;
using Brokerage.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

var dynamoDbConfig = new AmazonDynamoDBConfig
{
    ServiceURL = "http://localhost:4566",
    AuthenticationRegion = "us-east-1" 
};

var credentials = new BasicAWSCredentials("test", "test");
var dynamoClient = new AmazonDynamoDBClient(credentials, dynamoDbConfig);

builder.Services.AddSingleton<IAmazonDynamoDB>(dynamoClient);
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

    c.IncludeXmlComments(xmlPath);
});
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
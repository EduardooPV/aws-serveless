using Amazon.Lambda.Core;
using Brokerage.Domain.Messages;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Brokerage.Functions;

public class BalanceFunctions
{
    public static object ValidateBalance(OrderCreatedMessage input, ILambdaContext context)
    {
        context.Logger.LogInformation($"Validando saldo para a ordem: {input.OrderId}");

        return new
        {
            OrderId = input.OrderId,
            SaldoValido = true,
            Timestamp = DateTime.UtcNow
        };
    }
}

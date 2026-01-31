namespace Brokerage.Domain.Messages;

public sealed class OrderCreatedMessage
{
    public string OrderId { get; init; } = default!;
}

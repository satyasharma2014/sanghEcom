namespace BuildingBlocks.Contracts.Orders;

public static class OrderRoutingKeys
{
    public const string OrderPlaced = "order.placed";
}

public sealed record OrderPlacedEvent(
    Guid OrderId,
    string CustomerEmail,
    decimal TotalAmount,
    DateTime OccurredAtUtc);

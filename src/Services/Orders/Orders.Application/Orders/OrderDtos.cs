namespace Orders.Application.Orders;

public sealed record OrderLineRequest(string Sku, int Quantity, decimal UnitPrice);

public sealed record PlaceOrderRequest(
    Guid OrderId,
    string CustomerEmail,
    IReadOnlyList<OrderLineRequest> Lines);

public sealed record OrderDto(
    Guid OrderId,
    string CustomerEmail,
    decimal TotalAmount,
    string Status,
    string? FailureReason,
    DateTime CreatedAtUtc);

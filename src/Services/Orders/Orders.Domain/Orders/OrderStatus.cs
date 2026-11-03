namespace Orders.Domain.Orders;

public enum OrderStatus
{
    Pending = 1,
    StockReserved = 2,
    PaymentCaptured = 3,
    Confirmed = 4,
    Rejected = 5,
    Cancelled = 6,
    CompensationPending = 7
}

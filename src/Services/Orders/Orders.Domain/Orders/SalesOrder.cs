using BuildingBlocks.Common.Results;

namespace Orders.Domain.Orders;

public sealed class SalesOrder
{
    private readonly List<OrderLine> _lines = [];

    private SalesOrder() { }

    private SalesOrder(Guid id, string customerEmail, IEnumerable<OrderLine> lines)
    {
        Id = id;
        CustomerEmail = customerEmail;
        _lines.AddRange(lines);
        TotalAmount = _lines.Sum(l => l.UnitPrice * l.Quantity);
        Status = OrderStatus.Pending;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string CustomerEmail { get; private set; } = string.Empty;
    public decimal TotalAmount { get; private set; }
    public OrderStatus Status { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();

    public static Result<SalesOrder> Create(Guid id, string customerEmail, IReadOnlyList<OrderLine> lines)
    {
        if (id == Guid.Empty)
        {
            return Result.Failure<SalesOrder>(new Error("ORDER_INVALID", "OrderId khali nahi ho sakta."));
        }

        if (lines.Count == 0)
        {
            return Result.Failure<SalesOrder>(new Error("ORDER_INVALID", "Kam se kam ek item chahiye."));
        }

        if (lines.Select(l => l.Sku).Distinct().Count() != lines.Count)
        {
            return Result.Failure<SalesOrder>(new Error("ORDER_DUPLICATE_SKU", "Ek order mein same SKU do baar nahi."));
        }

        if (lines.Any(l => l.Quantity <= 0 || l.UnitPrice <= 0))
        {
            return Result.Failure<SalesOrder>(new Error("ORDER_INVALID_LINE", "Quantity aur price 0 se zyada hon."));
        }

        return Result.Success(new SalesOrder(id, customerEmail, lines));
    }

    // Allowed transitions (Saga steps)
    public void MarkStockReserved() => Move(OrderStatus.Pending, OrderStatus.StockReserved);
    public void MarkPaymentCaptured() => Move(OrderStatus.StockReserved, OrderStatus.PaymentCaptured);
    public void Confirm() => Move(OrderStatus.PaymentCaptured, OrderStatus.Confirmed);

    public void Cancel(string reason)
    {
        if (Status == OrderStatus.Confirmed)
        {
            throw new InvalidOperationException($"Confirmed order {Id} cancel nahi ho sakta.");
        }

        Status = OrderStatus.Cancelled;
        FailureReason = reason;
    }

    public void MarkCompensationPending(string reason)
    {
        if (Status == OrderStatus.Confirmed)
        {
            throw new InvalidOperationException($"Confirmed order {Id} compensation pending nahi ho sakta.");
        }

        Status = OrderStatus.CompensationPending;
        FailureReason = reason;
    }

    private void Move(OrderStatus from, OrderStatus to)
    {
        if (Status != from)
        {
            throw new InvalidOperationException($"Order {Id}: {Status} se {to} allowed nahi hai.");
        }

        Status = to;
    }
    public int CompensationAttempts { get; private set; }
    public DateTime? LastCompensationAttemptUtc { get; private set; }

    public void RecordCompensationAttempt(string error)
    {
        CompensationAttempts++;
        LastCompensationAttemptUtc = DateTime.UtcNow;
        FailureReason = error;
    }

    public void MarkCancelledByRecovery(string reason)
    {
        if (Status == OrderStatus.Confirmed)
        {
            throw new InvalidOperationException($"Confirmed order {Id} cancel nahi ho sakta.");
        }

        Status = OrderStatus.Cancelled;
        FailureReason = reason;
    }
}

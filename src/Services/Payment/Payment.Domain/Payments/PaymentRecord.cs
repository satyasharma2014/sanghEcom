using BuildingBlocks.Common.Results;

namespace Payment.Domain.Payments;

public enum PaymentStatus
{
    Captured = 1,
    Refunded = 2
}

public sealed class PaymentRecord
{
    private PaymentRecord() { }

    private PaymentRecord(Guid orderId, decimal amount, string transactionId)
    {
        Id = Guid.NewGuid();
        OrderId = orderId;
        Amount = amount;
        TransactionId = transactionId;
        Status = PaymentStatus.Captured;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public decimal Amount { get; private set; }
    public string TransactionId { get; private set; } = string.Empty;
    public PaymentStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static Result<PaymentRecord> Capture(Guid orderId, decimal amount, string transactionId)
    {
        if (amount <= 0)
        {
            return Result.Failure<PaymentRecord>(PaymentErrors.InvalidAmount);
        }

        return Result.Success(new PaymentRecord(orderId, amount, transactionId));
    }

    public void MarkRefunded() => Status = PaymentStatus.Refunded;
}

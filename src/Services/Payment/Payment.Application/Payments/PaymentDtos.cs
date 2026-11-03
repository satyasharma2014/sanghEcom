namespace Payment.Application.Payments;

public sealed record ChargePaymentCommand(Guid OrderId, decimal Amount);

public sealed record PaymentDto(
    Guid PaymentId,
    Guid OrderId,
    decimal Amount,
    string Status,
    string TransactionId,
    DateTime CreatedAtUtc);

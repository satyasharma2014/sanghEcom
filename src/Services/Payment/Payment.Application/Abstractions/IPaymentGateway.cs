namespace Payment.Application.Abstractions;

public sealed record GatewayResult(bool IsSuccess, string TransactionId, string? Reason);

public interface IPaymentGateway
{
    /// <summary>OrderId ko idempotency key ki tarah bhejte hain.</summary>
    Task<GatewayResult> ChargeAsync(Guid orderId, decimal amount, CancellationToken ct);
    Task<GatewayResult> RefundAsync(string transactionId, CancellationToken ct);
}

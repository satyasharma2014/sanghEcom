using System.Collections.Concurrent;
using Payment.Application.Abstractions;

namespace Payment.Infrastructure.Gateway;

public sealed class MockPaymentGateway : IPaymentGateway
{
    private const decimal MockLimit = 50000m;

    // Same orderId par pehla result (gateway-level idempotency)
    private readonly ConcurrentDictionary<Guid, GatewayResult> _charges = new();

    public Task<GatewayResult> ChargeAsync(Guid orderId, decimal amount, CancellationToken ct)
    {
        if (_charges.TryGetValue(orderId, out var existing))
        {
            return Task.FromResult(existing);
        }

        if (amount > MockLimit)
        {
            return Task.FromResult(new GatewayResult(false, string.Empty, "Card limit exceeded (mock)"));
        }

        var result = new GatewayResult(true, $"txn_{Guid.NewGuid():N}", null);
        return Task.FromResult(_charges.GetOrAdd(orderId, result));
    }

    public Task<GatewayResult> RefundAsync(string transactionId, CancellationToken ct) =>
        Task.FromResult(new GatewayResult(true, transactionId, null));
}

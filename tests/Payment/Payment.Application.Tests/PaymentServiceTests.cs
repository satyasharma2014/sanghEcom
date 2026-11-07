using BuildingBlocks.Common.Results;
using Payment.Application.Abstractions;
using Payment.Application.Payments;
using Payment.Domain.Payments;
using Xunit;

namespace Payment.Application.Tests;

public sealed class PaymentServiceTests
{
    private static (PaymentService svc, FakeGateway gw, FakePaymentRepo repo) Build()
    {
        var gw = new FakeGateway();
        var repo = new FakePaymentRepo();
        return (new PaymentService(repo, gw, new FakeUow()), gw, repo);
    }

    [Fact]
    public async Task Charge_Success_SavesOnce()
    {
        var (svc, _, repo) = Build();

        var result = await svc.ChargeAsync(new ChargePaymentCommand(Guid.NewGuid(), 5000m));

        Assert.True(result.IsSuccess);
        Assert.Single(repo.Items);
    }

    [Fact]
    public async Task Charge_SameOrderTwice_DoesNotChargeAgain()
    {
        var (svc, gw, _) = Build();
        var orderId = Guid.NewGuid();

        await svc.ChargeAsync(new ChargePaymentCommand(orderId, 5000m));
        await svc.ChargeAsync(new ChargePaymentCommand(orderId, 5000m));

        Assert.Equal(1, gw.ChargeCalls);
    }

    [Fact]
    public async Task Charge_Declined_ReturnsError_AndSavesNothing()
    {
        var (svc, _, repo) = Build();

        var result = await svc.ChargeAsync(new ChargePaymentCommand(Guid.NewGuid(), 60000m));

        Assert.Equal("PAYMENT_DECLINED", result.Error.Code);
        Assert.Empty(repo.Items);
    }

    [Fact]
    public async Task Refund_TwiceReturnsSuccess_AndRefundsOnlyOnce()
    {
        var (svc, gw, _) = Build();
        var orderId = Guid.NewGuid();
        await svc.ChargeAsync(new ChargePaymentCommand(orderId, 5000m));

        await svc.RefundAsync(orderId);
        var second = await svc.RefundAsync(orderId);

        Assert.True(second.IsSuccess);
        Assert.Equal(1, gw.RefundCalls);
        Assert.Equal("Refunded", second.Value.Status);
    }
}

internal sealed class FakeGateway : IPaymentGateway
{
    public int ChargeCalls { get; private set; }
    public int RefundCalls { get; private set; }

    public Task<GatewayResult> ChargeAsync(Guid orderId, decimal amount, CancellationToken ct)
    {
        ChargeCalls++;
        return Task.FromResult(amount > 50000m
            ? new GatewayResult(false, string.Empty, "limit")
            : new GatewayResult(true, $"txn_{Guid.NewGuid():N}", null));
    }

    public Task<GatewayResult> RefundAsync(string transactionId, CancellationToken ct)
    {
        RefundCalls++;
        return Task.FromResult(new GatewayResult(true, transactionId, null));
    }
}

internal sealed class FakePaymentRepo : IPaymentRepository
{
    public List<PaymentRecord> Items { get; } = [];

    public Task<PaymentRecord?> GetByOrderIdAsync(Guid orderId, CancellationToken ct) =>
        Task.FromResult(Items.FirstOrDefault(p => p.OrderId == orderId));

    public Task AddAsync(PaymentRecord payment, CancellationToken ct)
    {
        Items.Add(payment);
        return Task.CompletedTask;
    }
}

internal sealed class FakeUow : IUnitOfWork
{
    public Task<bool> TrySaveChangesAsync(CancellationToken ct) => Task.FromResult(true);
}

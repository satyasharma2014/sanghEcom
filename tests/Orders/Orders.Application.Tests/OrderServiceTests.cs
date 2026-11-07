using Orders.Application.Orders;
using Xunit;

namespace Orders.Application.Tests;

public sealed class OrderServiceTests
{
    private static (OrderService svc, FakeOrderRepository repo, FakeInventory inv, FakePayment pay, FakeOutbox outbox) Build()
    {
        var repo = new FakeOrderRepository();
        var inv = new FakeInventory();
        var pay = new FakePayment();
        var outbox = new FakeOutbox();

        var svc = new OrderService(
            repo,
            new FakeOrdersUow(),
            new FakeCatalog(),
            inv,
            pay,
            outbox);

        return (svc, repo, inv, pay, outbox);
    }

    private static PlaceOrderRequest Request(Guid id, string sku = "MUG-001", int qty = 2) =>
        new(id, "asha@example.com", [new OrderLineRequest(sku, qty, 1m)]);

    [Fact]
    public async Task Place_ValidOrder_IsConfirmed_WithServerPrice()
    {
        var (svc, repo, inv, pay, outbox) = Build();

        var result = await svc.PlaceAsync(Request(Guid.NewGuid()));

        Assert.True(result.IsSuccess);
        Assert.Equal("Confirmed", result.Value.Status);
        Assert.Equal(5000m, result.Value.TotalAmount);
        Assert.Equal(1, inv.ReserveCalls);
        Assert.Equal(1, pay.ChargeCalls);
        Assert.Single(outbox.Items);
        Assert.Single(repo.Orders);
    }

    [Fact]
    public async Task Place_SameOrderIdTwice_DoesNotCallDownstreamAgain()
    {
        var (svc, repo, inv, pay, _) = Build();
        var id = Guid.NewGuid();

        await svc.PlaceAsync(Request(id));
        var second = await svc.PlaceAsync(Request(id));

        Assert.True(second.IsSuccess);
        Assert.Equal(1, inv.ReserveCalls);
        Assert.Equal(1, pay.ChargeCalls);
        Assert.Single(repo.Orders);
    }

    [Fact]
    public async Task Place_PaymentDeclined_ReleasesStock_AndCancels()
    {
        var (svc, repo, inv, pay, outbox) = Build();
        pay.ChargeResult = BuildingBlocks.Common.Results.Result.Failure(
         new BuildingBlocks.Common.Results.Error("PAYMENT_DECLINED", "declined"));
        var id = Guid.NewGuid();

        var result = await svc.PlaceAsync(Request(id));

        Assert.Equal("PAYMENT_DECLINED", result.Error.Code);
        Assert.Equal("Cancelled", repo.Orders[id].Status.ToString());
        Assert.Equal(1, inv.ReleaseCalls);
        Assert.Equal(0, pay.RefundCalls);
        Assert.Empty(outbox.Items);
    }

    [Fact]
    public async Task Place_EmptyLines_Fails()
    {
        var (svc, _, _, _, _) = Build();
        var request = new PlaceOrderRequest(Guid.NewGuid(), "a@example.com", []);

        var result = await svc.PlaceAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal("ORDER_INVALID", result.Error.Code);
    }

    [Fact]
    public async Task Place_DuplicateSku_Fails()
    {
        var (svc, _, _, _, _) = Build();
        var request = new PlaceOrderRequest(Guid.NewGuid(), "a@example.com",
            [new OrderLineRequest("MUG-001", 1, 1m), new OrderLineRequest("mug-001", 1, 1m)]);

        var result = await svc.PlaceAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal("ORDER_DUPLICATE_SKU", result.Error.Code);
    }

    [Fact]
    public async Task Get_Missing_ReturnsNotFound()
    {
        var (svc, _, _, _, _) = Build();

        var result = await svc.GetAsync(Guid.NewGuid());

        Assert.Equal("ORDER_NOT_FOUND", result.Error.Code);
    }
}

using BuildingBlocks.Common.Results;
using Orders.Application.Abstractions;
using Orders.Domain.Orders;

namespace Orders.Application.Tests;

internal sealed class FakeOrderRepository : ISalesOrderRepository
{
    public Dictionary<Guid, SalesOrder> Orders { get; } = new();

    public Task<SalesOrder?> GetByIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Orders.GetValueOrDefault(id));

    public Task AddAsync(SalesOrder order, CancellationToken ct)
    {
        Orders[order.Id] = order;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<SalesOrder>> GetRecoveryCandidatesAsync(int maxAttempts, int batchSize, CancellationToken ct)
    {
        IReadOnlyList<SalesOrder> result = Orders.Values
            .Where(o => o.CompensationAttempts < maxAttempts && o.Status == OrderStatus.CompensationPending)
            .Take(batchSize)
            .ToList();

        return Task.FromResult(result);
    }
}

internal sealed class FakeOrdersUow : IOrdersUnitOfWork
{
    public Task<bool> TrySaveChangesAsync(CancellationToken ct) => Task.FromResult(true);
}

internal sealed class FakeCatalog : ICatalogClient
{
    public Dictionary<string, decimal> Prices { get; } = new() { ["MUG-001"] = 2500m };

    public Task<Result<decimal>> GetPriceAsync(string sku, CancellationToken ct) =>
        Task.FromResult(Prices.TryGetValue(sku, out var p)
            ? Result.Success(p)
            : Result.Failure<decimal>(new Error("ORDER_PRODUCT_NOT_FOUND", "SKU nahi mila.")));
}

internal sealed class FakeInventory : IInventoryClient
{
    public Result ReserveResult { get; set; } = Result.Success();
    public Result ReleaseResult { get; set; } = Result.Success();
    public int ReserveCalls { get; private set; }
    public int ReleaseCalls { get; private set; }

    public Task<Result> ReserveAsync(Guid orderId, IReadOnlyList<(string Sku, int Quantity)> lines, CancellationToken ct)
    {
        ReserveCalls++;
        return Task.FromResult(ReserveResult);
    }

    public Task<Result> ReleaseAsync(Guid orderId, CancellationToken ct)
    {
        ReleaseCalls++;
        return Task.FromResult(ReleaseResult);
    }
}

internal sealed class FakePayment : IPaymentClient
{
    public Result ChargeResult { get; set; } = Result.Success();
    public Result RefundResult { get; set; } = Result.Success();
    public int ChargeCalls { get; private set; }
    public int RefundCalls { get; private set; }

    public Task<Result> ChargeAsync(Guid orderId, decimal amount, CancellationToken ct)
    {
        ChargeCalls++;
        return Task.FromResult(ChargeResult);
    }

    public Task<Result> RefundAsync(Guid orderId, CancellationToken ct)
    {
        RefundCalls++;
        return Task.FromResult(RefundResult);
    }
}

internal sealed class FakeOutbox : IOutboxWriter
{
    public List<object> Items { get; } = [];

    public void Add<T>(string routingKey, T payload) => Items.Add(payload!);
}

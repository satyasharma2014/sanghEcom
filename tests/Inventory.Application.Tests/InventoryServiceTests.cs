using BuildingBlocks.Common.Results;
using Inventory.Application.Abstractions;
using Inventory.Application.Stock;
using Inventory.Domain.Stock;
using Xunit;

namespace Inventory.Application.Tests;

public sealed class InventoryServiceTests
{
    private static (InventoryService svc, FakeStockRepository repo) Build(params (string sku, int qty)[] stock)
    {
        var repo = new FakeStockRepository();
        foreach (var (sku, qty) in stock)
        {
            repo.Items.Add(StockItem.Create(sku, qty).Value);
        }

        return (new InventoryService(repo, new FakeUow()), repo);
    }

    private static int Qty(FakeStockRepository repo, string sku) =>
        repo.Items.First(i => i.Sku == sku).AvailableQuantity;

    [Fact]
    public async Task Reserve_DecreasesStock()
    {
        var (svc, repo) = Build(("PHONE-001", 5));

        var result = await svc.ReserveAsync(new ReserveStockCommand(Guid.NewGuid(),
            [new StockLine("PHONE-001", 2)]));

        Assert.True(result.IsSuccess);
        Assert.Equal(3, Qty(repo, "PHONE-001"));
    }

    [Fact]
    public async Task Reserve_AllOrNothing_NoChangeWhenAnyItemShort()
    {
        var (svc, repo) = Build(("PHONE-001", 5), ("MUG-001", 1));

        var result = await svc.ReserveAsync(new ReserveStockCommand(Guid.NewGuid(),
            [new StockLine("PHONE-001", 2), new StockLine("MUG-001", 3)]));

        Assert.Equal("STOCK_INSUFFICIENT", result.Error.Code);
        Assert.Equal(5, Qty(repo, "PHONE-001"));
        Assert.Empty(repo.Reservations);
    }

    [Fact]
    public async Task Reserve_SameOrderTwice_IsIdempotent()
    {
        var (svc, repo) = Build(("PHONE-001", 5));
        var cmd = new ReserveStockCommand(Guid.NewGuid(), [new StockLine("PHONE-001", 2)]);

        await svc.ReserveAsync(cmd);
        await svc.ReserveAsync(cmd);

        Assert.Equal(3, Qty(repo, "PHONE-001"));
    }

    [Fact]
    public async Task Release_RestoresStock_AndSecondReleaseDoesNotAddAgain()
    {
        var (svc, repo) = Build(("PHONE-001", 5));
        var orderId = Guid.NewGuid();
        await svc.ReserveAsync(new ReserveStockCommand(orderId, [new StockLine("PHONE-001", 2)]));

        await svc.ReleaseAsync(orderId);
        await svc.ReleaseAsync(orderId);

        Assert.Equal(5, Qty(repo, "PHONE-001"));
    }
}

internal sealed class FakeUow : IUnitOfWork
{
    public Task<bool> TrySaveChangesAsync(CancellationToken ct) => Task.FromResult(true);
}

internal sealed class FakeStockRepository : IStockRepository
{
    public List<StockItem> Items { get; } = [];
    public List<StockReservation> Reservations { get; } = [];

    public Task<StockItem?> GetBySkuAsync(string sku, CancellationToken ct) =>
        Task.FromResult(Items.FirstOrDefault(i => i.Sku == sku));

    public Task<IReadOnlyList<StockItem>> GetBySkusAsync(IReadOnlyList<string> skus, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<StockItem>>(Items.Where(i => skus.Contains(i.Sku)).ToList());

    public Task<bool> ExistsAsync(string sku, CancellationToken ct) =>
        Task.FromResult(Items.Any(i => i.Sku == sku));

    public Task AddItemAsync(StockItem item, CancellationToken ct)
    {
        Items.Add(item);
        return Task.CompletedTask;
    }

    public Task<bool> ReservationExistsAsync(Guid orderId, CancellationToken ct) =>
        Task.FromResult(Reservations.Any(r => r.OrderId == orderId));

    public Task<IReadOnlyList<StockReservation>> GetActiveReservationsAsync(Guid orderId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<StockReservation>>(
            Reservations.Where(r => r.OrderId == orderId && r.Status == ReservationStatus.Reserved).ToList());

    public Task AddReservationsAsync(IEnumerable<StockReservation> reservations, CancellationToken ct)
    {
        Reservations.AddRange(reservations);
        return Task.CompletedTask;
    }
}

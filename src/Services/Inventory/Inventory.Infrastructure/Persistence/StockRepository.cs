using Inventory.Application.Abstractions;
using Inventory.Domain.Stock;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence;

public sealed class StockRepository(InventoryDbContext db) : IStockRepository
{
    public Task<StockItem?> GetBySkuAsync(string sku, CancellationToken ct) =>
        db.StockItems.FirstOrDefaultAsync(s => s.Sku == sku, ct);

    public async Task<IReadOnlyList<StockItem>> GetBySkusAsync(IReadOnlyList<string> skus, CancellationToken ct) =>
        await db.StockItems.Where(s => skus.Contains(s.Sku)).ToListAsync(ct);

    public Task<bool> ExistsAsync(string sku, CancellationToken ct) =>
        db.StockItems.AnyAsync(s => s.Sku == sku, ct);

    public async Task AddItemAsync(StockItem item, CancellationToken ct) =>
        await db.StockItems.AddAsync(item, ct);

    public Task<bool> ReservationExistsAsync(Guid orderId, CancellationToken ct) =>
        db.StockReservations.AnyAsync(r => r.OrderId == orderId, ct);

    public async Task<IReadOnlyList<StockReservation>> GetActiveReservationsAsync(Guid orderId, CancellationToken ct) =>
        await db.StockReservations
            .Where(r => r.OrderId == orderId && r.Status == ReservationStatus.Reserved)
            .ToListAsync(ct);

    public Task AddReservationsAsync(IEnumerable<StockReservation> reservations, CancellationToken ct)
    {
        db.StockReservations.AddRange(reservations);
        return Task.CompletedTask;
    }
}

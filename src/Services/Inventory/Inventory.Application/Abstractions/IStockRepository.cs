using Inventory.Domain.Stock;

namespace Inventory.Application.Abstractions;

public interface IStockRepository
{
    Task<StockItem?> GetBySkuAsync(string sku, CancellationToken ct);
    Task<IReadOnlyList<StockItem>> GetBySkusAsync(IReadOnlyList<string> skus, CancellationToken ct);
    Task<bool> ExistsAsync(string sku, CancellationToken ct);
    Task AddItemAsync(StockItem item, CancellationToken ct);

    Task<bool> ReservationExistsAsync(Guid orderId, CancellationToken ct);
    Task<IReadOnlyList<StockReservation>> GetActiveReservationsAsync(Guid orderId, CancellationToken ct);
    Task AddReservationsAsync(IEnumerable<StockReservation> reservations, CancellationToken ct);
}

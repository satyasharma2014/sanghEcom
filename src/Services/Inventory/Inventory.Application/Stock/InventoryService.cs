using BuildingBlocks.Common.Results;
using Inventory.Application.Abstractions;
using Inventory.Domain.Stock;

namespace Inventory.Application.Stock;

public sealed class InventoryService(IStockRepository repository, IUnitOfWork uow)
{
    public async Task<Result<StockDto>> GetStockAsync(string sku, CancellationToken ct = default)
    {
        var normalized = sku.Trim().ToUpperInvariant();
        var item = await repository.GetBySkuAsync(normalized, ct);

        return item is null
            ? Result.Failure<StockDto>(StockErrors.NotFound(normalized))
            : Result.Success(new StockDto(item.Sku, item.AvailableQuantity));
    }

    /// <summary>
    /// Sab items ek saath reserve hote hain, ya koi nahi (all-or-nothing).
    /// Same OrderId dobara aaye to kuch nahi badlega (idempotent).
    /// </summary>
    public async Task<Result> ReserveAsync(ReserveStockCommand command, CancellationToken ct = default)
    {
        if (await repository.ReservationExistsAsync(command.OrderId, ct))
        {
            return Result.Success();
        }

        // Same SKU ki lines ko jod do
        var lines = command.Items
            .GroupBy(i => i.Sku.Trim().ToUpperInvariant())
            .Select(g => new StockLine(g.Key, g.Sum(x => x.Quantity)))
            .ToList();

        var items = await repository.GetBySkusAsync(lines.Select(l => l.Sku).ToList(), ct);

        // Pass 1: sab check karo, kuch change nahi
        foreach (var line in lines)
        {
            var item = items.FirstOrDefault(x => x.Sku == line.Sku);
            if (item is null)
            {
                return Result.Failure(StockErrors.NotFound(line.Sku));
            }

            if (item.AvailableQuantity < line.Quantity)
            {
                return Result.Failure(StockErrors.InsufficientStock);
            }
        }

        // Pass 2: ab reserve karo
        var reservations = new List<StockReservation>();
        foreach (var line in lines)
        {
            var item = items.First(x => x.Sku == line.Sku);
            item.Reserve(line.Quantity);
            reservations.Add(new StockReservation(command.OrderId, line.Sku, line.Quantity));
        }

        await repository.AddReservationsAsync(reservations, ct);

        return await uow.TrySaveChangesAsync(ct)
            ? Result.Success()
            : Result.Failure(new Error("STOCK_CONCURRENCY_CONFLICT", "Stock abhi badla gaya. Dobara try karein."));
    }

    /// <summary>Order cancel par stock wapas. Dobara call par kuch nahi hota.</summary>
    public async Task<Result> ReleaseAsync(Guid orderId, CancellationToken ct = default)
    {
        var reservations = await repository.GetActiveReservationsAsync(orderId, ct);
        if (reservations.Count == 0)
        {
            return Result.Success();
        }

        var items = await repository.GetBySkusAsync(reservations.Select(r => r.Sku).Distinct().ToList(), ct);

        foreach (var reservation in reservations)
        {
            var item = items.First(x => x.Sku == reservation.Sku);
            item.Release(reservation.Quantity);
            reservation.MarkReleased();
        }

        return await uow.TrySaveChangesAsync(ct)
            ? Result.Success()
            : Result.Failure(new Error("STOCK_CONCURRENCY_CONFLICT", "Stock abhi badla gaya. Dobara try karein."));
    }
}

namespace Inventory.Application.Stock;

public sealed record StockLine(string Sku, int Quantity);

public sealed record ReserveStockCommand(Guid OrderId, IReadOnlyList<StockLine> Items);

public sealed record StockDto(string Sku, int AvailableQuantity);

using BuildingBlocks.Common.Results;

namespace Inventory.Domain.Stock;

public static class StockErrors
{
    public static readonly Error InsufficientStock =
        new("STOCK_INSUFFICIENT", "Stock kam hai.");

    public static readonly Error InvalidQuantity =
        new("STOCK_INVALID_QUANTITY", "Quantity 0 se zyada honi chahiye.");

    public static Error NotFound(string sku) =>
        new("STOCK_ITEM_NOT_FOUND", $"SKU '{sku}' ka stock record nahi mila.");
}

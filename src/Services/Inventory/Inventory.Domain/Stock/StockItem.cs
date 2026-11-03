using BuildingBlocks.Common.Results;

namespace Inventory.Domain.Stock;

public sealed class StockItem
{
    private StockItem() { }

    private StockItem(string sku, int quantity)
    {
        Id = Guid.NewGuid();
        Sku = sku;
        AvailableQuantity = quantity;
    }

    public Guid Id { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public int AvailableQuantity { get; private set; }

    public static Result<StockItem> Create(string sku, int quantity)
    {
        if (string.IsNullOrWhiteSpace(sku) || quantity < 0)
        {
            return Result.Failure<StockItem>(StockErrors.InvalidQuantity);
        }

        return Result.Success(new StockItem(sku.Trim().ToUpperInvariant(), quantity));
    }

    public Result Reserve(int quantity)
    {
        if (quantity <= 0)
        {
            return Result.Failure(StockErrors.InvalidQuantity);
        }

        if (AvailableQuantity < quantity)
        {
            return Result.Failure(StockErrors.InsufficientStock);
        }

        AvailableQuantity -= quantity;
        return Result.Success();
    }

    public void Release(int quantity) => AvailableQuantity += quantity;
}

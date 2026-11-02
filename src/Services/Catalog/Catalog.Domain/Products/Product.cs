using BuildingBlocks.Common.Results;

namespace Catalog.Domain.Products;

public sealed class Product
{
    // Database aur EF Core ke liye (app code ise seedha use nahi karega)
    private Product() { }

    private Product(Guid id, string sku, string name, string description, decimal price)
    {
        Id = id;
        Sku = sku;
        Name = name;
        Description = description;
        Price = price;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// Product banane ka ekmaatra rasta. Business rules yahin check hote hain.
    /// </summary>
    public static Result<Product> Create(string sku, string name, string description, decimal price)
    {
        if (string.IsNullOrWhiteSpace(sku))
            return Result.Failure<Product>(ProductErrors.InvalidSku);

        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<Product>(ProductErrors.InvalidName);

        if (price <= 0)
            return Result.Failure<Product>(ProductErrors.InvalidPrice);

        var product = new Product(
            Guid.NewGuid(),
            sku.Trim().ToUpperInvariant(),
            name.Trim(),
            description?.Trim() ?? string.Empty,
            price);

        return Result.Success(product);
    }
}

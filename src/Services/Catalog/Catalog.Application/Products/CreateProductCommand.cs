namespace Catalog.Application.Products;

public sealed record CreateProductCommand(
    string Sku,
    string Name,
    string Description,
    decimal Price);

public sealed record ProductDto(
    Guid Id,
    string Sku,
    string Name,
    string Description,
    decimal Price,
    DateTime CreatedAtUtc);

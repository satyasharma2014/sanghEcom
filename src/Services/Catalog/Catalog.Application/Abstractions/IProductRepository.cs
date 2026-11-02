using Catalog.Domain.Products;

namespace Catalog.Application.Abstractions;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Product?> GetBySkuAsync(string sku, CancellationToken ct);
    Task<bool> SkuExistsAsync(string sku, CancellationToken ct);
    Task AddAsync(Product product, CancellationToken ct);
}

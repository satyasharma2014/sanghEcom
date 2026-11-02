using BuildingBlocks.Common.Results;
using Catalog.Application.Abstractions;
using Microsoft.Extensions.Caching.Memory;

namespace Catalog.Application.Products;

public sealed class ProductQueryService(IProductRepository repository, IMemoryCache cache)
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public async Task<Result<ProductDto>> GetBySkuAsync(string sku, CancellationToken ct = default)
    {
        var key = $"product:sku:{sku}";

        if (cache.TryGetValue(key, out ProductDto? cached) && cached is not null)
        {
            return Result.Success(cached);
        }

        var product = await repository.GetBySkuAsync(sku, ct);
        if (product is null)
        {
            return Result.Failure<ProductDto>(new Error("PRODUCT_NOT_FOUND", "Product nahi mila."));
        }

        var dto = new ProductDto(product.Id, product.Sku, product.Name, product.Description, product.Price, product.CreatedAtUtc);
        cache.Set(key, dto, CacheTtl);

        return Result.Success(dto);
    }
}

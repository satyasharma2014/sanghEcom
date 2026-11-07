using Catalog.Application.Abstractions;
using Catalog.Application.Products;
using Catalog.Domain.Products;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace Catalog.Application.Tests;

public sealed class ProductQueryServiceTests
{
    [Fact]
    public async Task SecondCall_IsServedFromCache()
    {
        var repo = new CountingRepository();
        repo.Products.Add(Product.Create("MUG-001", "Mug", "", 2500m).Value);

        using var cache = new MemoryCache(new MemoryCacheOptions());
        var query = new ProductQueryService(repo, cache);

        await query.GetBySkuAsync("MUG-001");
        await query.GetBySkuAsync("MUG-001");

        Assert.Equal(1, repo.GetBySkuCalls);
    }

    [Fact]
    public async Task NotFound_IsNotCached()
    {
        var repo = new CountingRepository();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var query = new ProductQueryService(repo, cache);

        await query.GetBySkuAsync("NOPE-1");
        repo.Products.Add(Product.Create("NOPE-1", "New", "", 100m).Value);
        var after = await query.GetBySkuAsync("NOPE-1");

        Assert.True(after.IsSuccess);
    }
}

internal sealed class CountingRepository : IProductRepository
{
    public List<Product> Products { get; } = [];
    public int GetBySkuCalls { get; private set; }

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Products.FirstOrDefault(p => p.Id == id));

    public Task<Product?> GetBySkuAsync(string sku, CancellationToken ct)
    {
        GetBySkuCalls++;
        return Task.FromResult(Products.FirstOrDefault(p => p.Sku == sku));
    }

    public Task<bool> SkuExistsAsync(string sku, CancellationToken ct) =>
        Task.FromResult(Products.Any(p => p.Sku == sku));

    public Task AddAsync(Product product, CancellationToken ct)
    {
        Products.Add(product);
        return Task.CompletedTask;
    }
}

using Catalog.Application.Abstractions;
using Catalog.Domain.Products;

namespace Catalog.Application.Tests;

internal sealed class FakeProductRepository : IProductRepository
{
    public List<Product> Products { get; } = [];

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Products.FirstOrDefault(p => p.Id == id));

    public Task<Product?> GetBySkuAsync(string sku, CancellationToken ct) =>
        Task.FromResult(Products.FirstOrDefault(p => p.Sku == sku));

    public Task<bool> SkuExistsAsync(string sku, CancellationToken ct) =>
        Task.FromResult(Products.Any(p => p.Sku == sku));

    public Task AddAsync(Product product, CancellationToken ct)
    {
        Products.Add(product);
        return Task.CompletedTask;
    }
}

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
}

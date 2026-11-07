using Catalog.Application.Products;
using Xunit;

namespace Catalog.Application.Tests;

public sealed class ProductServiceTests
{
    private static ProductService Build(FakeProductRepository repo) =>
        new(repo, new FakeUnitOfWork());

    [Fact]
    public void Product_ZeroPrice_IsRejected()
    {
        var result = Catalog.Domain.Products.Product.Create("MUG-1", "Mug", "", 0m);

        Assert.True(result.IsFailure);
        Assert.Equal("PRODUCT_INVALID_PRICE", result.Error.Code);
    }

    [Fact]
    public async Task Create_ValidProduct_IsSaved_AndSkuIsUpperCase()
    {
        var repo = new FakeProductRepository();

        var result = await Build(repo).CreateAsync(
            new CreateProductCommand("mug-001", "Coffee Mug", "350ml", 2500m));

        Assert.True(result.IsSuccess);
        Assert.Equal("MUG-001", result.Value.Sku);
        Assert.Single(repo.Products);
    }

    [Fact]
    public async Task Create_DuplicateSku_Fails()
    {
        var repo = new FakeProductRepository();
        var service = Build(repo);
        await service.CreateAsync(new CreateProductCommand("MUG-001", "Mug", "", 2500m));

        var result = await service.CreateAsync(new CreateProductCommand("MUG-001", "Mug 2", "", 3000m));

        Assert.Equal("PRODUCT_SKU_EXISTS", result.Error.Code);
        Assert.Single(repo.Products);
    }

    [Fact]
    public async Task GetBySku_Missing_ReturnsNotFound()
    {
        var result = await Build(new FakeProductRepository()).GetBySkuAsync("NOPE-1");

        Assert.Equal("PRODUCT_NOT_FOUND", result.Error.Code);
    }
}

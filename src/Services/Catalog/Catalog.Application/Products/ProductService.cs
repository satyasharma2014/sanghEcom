using BuildingBlocks.Common.Results;
using Catalog.Application.Abstractions;
using Catalog.Domain.Products;

namespace Catalog.Application.Products;

public sealed class ProductService(IProductRepository repository, IUnitOfWork unitOfWork)
{
    public async Task<Result<ProductDto>> CreateAsync(CreateProductCommand command, CancellationToken ct = default)
    {
        var product = Product.Create(command.Sku, command.Name, command.Description, command.Price);
        if (product.IsFailure)
            return Result.Failure<ProductDto>(product.Error);

        if (await repository.SkuExistsAsync(product.Value.Sku, ct))
            return Result.Failure<ProductDto>(new Error("PRODUCT_SKU_EXISTS", "Is SKU ka product pehle se hai."));

        await repository.AddAsync(product.Value, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success(ToDto(product.Value));
    }

    public async Task<Result<ProductDto>> GetBySkuAsync(string sku, CancellationToken ct = default)
    {
        var product = await repository.GetBySkuAsync(sku.Trim().ToUpperInvariant(), ct);

        return product is null
            ? Result.Failure<ProductDto>(new Error("PRODUCT_NOT_FOUND", "Product nahi mila."))
            : Result.Success(ToDto(product));
    }

    private static ProductDto ToDto(Product p) =>
        new(p.Id, p.Sku, p.Name, p.Description, p.Price, p.CreatedAtUtc);
}

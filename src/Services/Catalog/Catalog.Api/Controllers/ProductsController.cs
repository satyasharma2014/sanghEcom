using BuildingBlocks.Common.Results;
using Catalog.Application.Products;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Controllers;

[ApiController]
[Route("api/v1/products")]
public sealed class ProductsController(ProductService service) : ControllerBase
{
    // POST api/v1/products
    [HttpPost]
    public async Task<IActionResult> Create(CreateProductCommand command, CancellationToken ct)
    {
        var result = await service.CreateAsync(command, ct);

        if (result.IsFailure)
        {
            return ToProblem(result.Error);
        }

        return CreatedAtAction(nameof(GetBySku), new { sku = result.Value.Sku }, result.Value);
    }

    // GET api/v1/products/sku/MUG-001
    [HttpGet("sku/{sku}")]
    public async Task<IActionResult> GetBySku(string sku, CancellationToken ct)
    {
        var result = await service.GetBySkuAsync(sku, ct);

        return result.IsFailure
            ? ToProblem(result.Error)
            : Ok(result.Value);
    }

    // Business error code ko HTTP status mein badalta hai
    private IActionResult ToProblem(Error error)
    {
        var status = error.Code switch
        {
            "PRODUCT_NOT_FOUND" => StatusCodes.Status404NotFound,
            "PRODUCT_SKU_EXISTS" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };

        return Problem(
            title: "Request failed",
            detail: error.Message,
            statusCode: status,
            extensions: new Dictionary<string, object?> { ["errorCode"] = error.Code });
    }
}

using BuildingBlocks.Common.Results;
using Inventory.Application.Stock;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers;

[ApiController]
[Route("api/v1/inventory")]
public sealed class StockController(InventoryService service) : ControllerBase
{
    [HttpGet("{sku}")]
    public async Task<IActionResult> Get(string sku, CancellationToken ct)
    {
        var result = await service.GetStockAsync(sku, ct);
        return result.IsFailure ? ToProblem(result.Error) : Ok(result.Value);
    }

    [HttpPost("reservations")]
    public async Task<IActionResult> Reserve(ReserveStockRequest request, CancellationToken ct)
    {
        var result = await service.ReserveAsync(
            new ReserveStockCommand(request.OrderId, request.Items), ct);

        return result.IsFailure ? ToProblem(result.Error) : Ok(new { request.OrderId, Status = "Reserved" });
    }

    [HttpPost("reservations/{orderId:guid}/release")]
    public async Task<IActionResult> Release(Guid orderId, CancellationToken ct)
    {
        var result = await service.ReleaseAsync(orderId, ct);
        return result.IsFailure ? ToProblem(result.Error) : NoContent();
    }

    private IActionResult ToProblem(Error error)
    {
        var status = error.Code switch
        {
            "STOCK_ITEM_NOT_FOUND" => StatusCodes.Status404NotFound,
            "STOCK_INSUFFICIENT" => StatusCodes.Status409Conflict,
            "STOCK_CONCURRENCY_CONFLICT" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };

        return Problem(
            title: "Request failed",
            detail: error.Message,
            statusCode: status,
            extensions: new Dictionary<string, object?> { ["errorCode"] = error.Code });
    }
}

public sealed record ReserveStockRequest(Guid OrderId, List<StockLine> Items);

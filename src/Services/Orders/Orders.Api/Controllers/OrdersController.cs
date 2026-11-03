using BuildingBlocks.Common.Results;
using Microsoft.AspNetCore.Mvc;
using Orders.Application.Orders;

namespace Orders.Api.Controllers;

[ApiController]
[Route("api/v1/orders")]
public sealed class OrdersController(OrderService service) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Place(PlaceOrderRequest request, CancellationToken ct)
    {
        var result = await service.PlaceAsync(request, ct);
        return result.IsFailure ? ToProblem(result.Error) : Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var result = await service.GetAsync(id, ct);
        return result.IsFailure ? ToProblem(result.Error) : Ok(result.Value);
    }

    private IActionResult ToProblem(Error error)
    {
        var status = error.Code switch
        {
            //"ORDER_NOT_FOUND" => StatusCodes.Status404NotFound,
            //"ORDER_CONFLICT" => StatusCodes.Status409Conflict,
            "ORDER_PRODUCT_NOT_FOUND" => StatusCodes.Status400BadRequest,
            "STOCK_INSUFFICIENT" => StatusCodes.Status409Conflict,
            "PAYMENT_DECLINED" => StatusCodes.Status402PaymentRequired,
            "DEPENDENCY_UNAVAILABLE" => StatusCodes.Status503ServiceUnavailable,
            "ORDER_COMPENSATION_PENDING" => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status400BadRequest
        };

        return Problem(
            title: "Order request failed",
            detail: error.Message,
            statusCode: status,
            extensions: new Dictionary<string, object?> { ["errorCode"] = error.Code });
    }
}

using BuildingBlocks.Common.Results;
using Microsoft.AspNetCore.Mvc;
using Payment.Application.Payments;

namespace Payment.Api.Controllers;

[ApiController]
[Route("api/v1/payments")]
public sealed class PaymentsController(PaymentService service) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Charge(ChargePaymentCommand command, CancellationToken ct)
    {
        var result = await service.ChargeAsync(command, ct);
        return result.IsFailure ? ToProblem(result.Error) : Ok(result.Value);
    }

    [HttpGet("orders/{orderId:guid}")]
    public async Task<IActionResult> Get(Guid orderId, CancellationToken ct)
    {
        var result = await service.GetByOrderIdAsync(orderId, ct);
        return result.IsFailure ? ToProblem(result.Error) : Ok(result.Value);
    }

    [HttpPost("orders/{orderId:guid}/refund")]
    public async Task<IActionResult> Refund(Guid orderId, CancellationToken ct)
    {
        var result = await service.RefundAsync(orderId, ct);
        return result.IsFailure ? ToProblem(result.Error) : Ok(result.Value);
    }

    private IActionResult ToProblem(Error error)
    {
        var status = error.Code switch
        {
            "PAYMENT_NOT_FOUND" => StatusCodes.Status404NotFound,
            "PAYMENT_DECLINED" => StatusCodes.Status402PaymentRequired,
            "PAYMENT_CONCURRENCY_CONFLICT" => StatusCodes.Status409Conflict,
            "PAYMENT_REFUND_FAILED" => StatusCodes.Status502BadGateway,
            _ => StatusCodes.Status400BadRequest
        };

        return Problem(
            title: "Payment request failed",
            detail: error.Message,
            statusCode: status,
            extensions: new Dictionary<string, object?> { ["errorCode"] = error.Code });
    }
}

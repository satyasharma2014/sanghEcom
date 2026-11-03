//using BuildingBlocks.Common.Results;
//using Orders.Application.Abstractions;
//using Orders.Domain.Orders;

//namespace Orders.Application.Orders;

//public sealed class OrderService(ISalesOrderRepository orders, IOrdersUnitOfWork uow)
//{
//    public async Task<Result<OrderDto>> PlaceAsync(PlaceOrderRequest request, CancellationToken ct = default)
//    {
//        // Idempotency: same OrderId dobara aaye to wahi order lautao
//        var existing = await orders.GetByIdAsync(request.OrderId, ct);
//        if (existing is not null)
//        {
//            return Result.Success(ToDto(existing));
//        }

//        var lines = request.Lines
//            .Select(l => new OrderLine(l.Sku.Trim().ToUpperInvariant(), l.Quantity, l.UnitPrice))
//            .ToList();

//        var created = SalesOrder.Create(request.OrderId, request.CustomerEmail.Trim(), lines);
//        if (created.IsFailure)
//        {
//            return Result.Failure<OrderDto>(created.Error);
//        }

//        await orders.AddAsync(created.Value, ct);

//        return await uow.TrySaveChangesAsync(ct)
//            ? Result.Success(ToDto(created.Value))
//            : Result.Failure<OrderDto>(new Error("ORDER_CONFLICT", "Order save nahi hua, dobara try karein."));
//    }

//    public async Task<Result<OrderDto>> GetAsync(Guid id, CancellationToken ct = default)
//    {
//        var order = await orders.GetByIdAsync(id, ct);
//        return order is null
//            ? Result.Failure<OrderDto>(new Error("ORDER_NOT_FOUND", "Order nahi mila."))
//            : Result.Success(ToDto(order));
//    }

//    private static OrderDto ToDto(SalesOrder o) =>
//        new(o.Id, o.CustomerEmail, o.TotalAmount, o.Status.ToString(), o.FailureReason, o.CreatedAtUtc);
//}
using BuildingBlocks.Common.Results;
using BuildingBlocks.Contracts;
using BuildingBlocks.Contracts.Orders;
using Orders.Application.Abstractions;
using Orders.Domain.Orders;

namespace Orders.Application.Orders;

public sealed class OrderService(
    ISalesOrderRepository orders,
    IOrdersUnitOfWork uow,
    ICatalogClient catalog,
    IInventoryClient inventory,
    IPaymentClient payment,
    IOutboxWriter outbox)
{
    public async Task<Result<OrderDto>> PlaceAsync(PlaceOrderRequest request, CancellationToken ct = default)
    {
        // Idempotency: same OrderId dobara aaye to wahi order lautao
        var existing = await orders.GetByIdAsync(request.OrderId, ct);
        if (existing is not null)
        {
            return Result.Success(ToDto(existing));
        }

        // 1. Price Catalog se (client ka price ignore)
        var lines = new List<OrderLine>();
        foreach (var line in request.Lines)
        {
            var sku = line.Sku.Trim().ToUpperInvariant();
            var price = await catalog.GetPriceAsync(sku, ct);
            if (price.IsFailure)
            {
                return Result.Failure<OrderDto>(price.Error);
            }

            lines.Add(new OrderLine(sku, line.Quantity, price.Value));
        }

        var created = SalesOrder.Create(request.OrderId, request.CustomerEmail.Trim(), lines);
        if (created.IsFailure)
        {
            return Result.Failure<OrderDto>(created.Error);
        }

        var order = created.Value;
        await orders.AddAsync(order, ct);
        if (!await uow.TrySaveChangesAsync(ct))
        {
            return Result.Failure<OrderDto>(new Error("ORDER_CONFLICT", "Order save nahi hua. Dobara try karein."));
        }

        // 2. Stock reserve
        var reserve = await inventory.ReserveAsync(order.Id,
            order.Lines.Select(l => (l.Sku, l.Quantity)).ToList(), ct);
        if (reserve.IsFailure)
        {
            return await FailAsync(order, reserve.Error, stockReserved: reserve.Error.Code == ErrorCodes.DependencyUnavailable, refund: false, ct);
        }

        order.MarkStockReserved();
        await uow.TrySaveChangesAsync(ct);

        // 3. Payment charge
        var charge = await payment.ChargeAsync(order.Id, order.TotalAmount, ct);
        if (charge.IsFailure)
        {
            var unknown = charge.Error.Code == ErrorCodes.DependencyUnavailable;
            return await FailAsync(order, charge.Error, stockReserved: true, refund: unknown, ct);
        }

        order.MarkPaymentCaptured();
        await uow.TrySaveChangesAsync(ct);

        // 4. Confirm + event (ek hi save mein)
        order.Confirm();
        outbox.Add(OrderRoutingKeys.OrderPlaced,
            new OrderPlacedEvent(order.Id, order.CustomerEmail, order.TotalAmount, DateTime.UtcNow));

        return await uow.TrySaveChangesAsync(ct)
            ? Result.Success(ToDto(order))
            : Result.Failure<OrderDto>(new Error("ORDER_CONFLICT", "Final save fail hua."));
    }

    public async Task<Result<OrderDto>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var order = await orders.GetByIdAsync(id, ct);
        return order is null
            ? Result.Failure<OrderDto>(new Error("ORDER_NOT_FOUND", "Order nahi mila."))
            : Result.Success(ToDto(order));
    }

    /// <summary>Compensation: refund (agar zarurat), stock release, phir Cancelled.</summary>
    private async Task<Result<OrderDto>> FailAsync(SalesOrder order, Error error, bool stockReserved, bool refund, CancellationToken ct)
    {
        if (refund)
        {
            var r = await payment.RefundAsync(order.Id, ct);
            if (r.IsFailure)
            {
                return await MarkPendingAsync(order, r.Error.Code, ct);
            }
        }

        if (stockReserved)
        {
            var r = await inventory.ReleaseAsync(order.Id, ct);
            if (r.IsFailure)
            {
                return await MarkPendingAsync(order, r.Error.Code, ct);
            }
        }

        order.Cancel(error.Code);
        await uow.TrySaveChangesAsync(ct);
        return Result.Failure<OrderDto>(error);
    }

    private async Task<Result<OrderDto>> MarkPendingAsync(SalesOrder order, string reason, CancellationToken ct)
    {
        order.MarkCompensationPending(reason);
        await uow.TrySaveChangesAsync(ct);
        return Result.Failure<OrderDto>(new Error("ORDER_COMPENSATION_PENDING", "Rollback pending hai."));
    }

    private static OrderDto ToDto(SalesOrder o) =>
        new(o.Id, o.CustomerEmail, o.TotalAmount, o.Status.ToString(), o.FailureReason, o.CreatedAtUtc);
}

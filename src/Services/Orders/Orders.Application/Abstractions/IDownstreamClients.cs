using BuildingBlocks.Common.Results;

namespace Orders.Application.Abstractions;

public interface ICatalogClient
{
    Task<Result<decimal>> GetPriceAsync(string sku, CancellationToken ct);
}

public interface IInventoryClient
{
    Task<Result> ReserveAsync(Guid orderId, IReadOnlyList<(string Sku, int Quantity)> lines, CancellationToken ct);
    Task<Result> ReleaseAsync(Guid orderId, CancellationToken ct);
}

public interface IPaymentClient
{
    Task<Result> ChargeAsync(Guid orderId, decimal amount, CancellationToken ct);
    Task<Result> RefundAsync(Guid orderId, CancellationToken ct);
}

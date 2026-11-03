using Microsoft.Extensions.Logging;
using Orders.Application.Abstractions;

namespace Orders.Application.Recovery;

public sealed class CompensationRecoveryService(
    ISalesOrderRepository orders,
    IOrdersUnitOfWork uow,
    IInventoryClient inventory,
    IPaymentClient payment,
    ILogger<CompensationRecoveryService> logger)
{
    /// <returns>Kitne orders process hue.</returns>
    public async Task<int> RunOnceAsync(int maxAttempts, int batchSize, CancellationToken ct)
    {
        var candidates = await orders.GetRecoveryCandidatesAsync(maxAttempts, batchSize, ct);

        foreach (var order in candidates)
        {
            // Dono idempotent: payment nahi hui to refund 404 (success), reservation nahi to release no-op
            var refund = await payment.RefundAsync(order.Id, ct);
            var release = refund.IsSuccess
                ? await inventory.ReleaseAsync(order.Id, ct)
                : refund;

            if (refund.IsSuccess && release.IsSuccess)
            {
                order.MarkCancelledByRecovery("RECOVERED: background job ne compensation complete kiya");
                logger.LogInformation("Order {OrderId} recover hua, status Cancelled.", order.Id);
            }
            else
            {
                var error = refund.IsFailure ? refund.Error.Code : release.Error.Code;
                order.RecordCompensationAttempt(error);

                if (order.CompensationAttempts >= maxAttempts)
                {
                    logger.LogCritical("MANUAL INTERVENTION: Order {OrderId} {Attempts} baar fail. Error: {Code}",
                        order.Id, order.CompensationAttempts, error);
                }
                else
                {
                    logger.LogWarning("Order {OrderId} recovery attempt {Attempts}/{Max} fail. Error: {Code}",
                        order.Id, order.CompensationAttempts, maxAttempts, error);
                }
            }

            await uow.TrySaveChangesAsync(ct);
        }

        return candidates.Count;
    }
}

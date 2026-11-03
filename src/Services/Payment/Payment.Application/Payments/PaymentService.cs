using BuildingBlocks.Common.Results;
using Payment.Application.Abstractions;
using Payment.Domain.Payments;

namespace Payment.Application.Payments;

public sealed class PaymentService(
    IPaymentRepository repository,
    IPaymentGateway gateway,
    IUnitOfWork uow)
{
    public async Task<Result<PaymentDto>> ChargeAsync(ChargePaymentCommand command, CancellationToken ct = default)
    {
        // Idempotency: is order ki payment pehle se hai to wahi result do
        var existing = await repository.GetByOrderIdAsync(command.OrderId, ct);
        if (existing is not null)
        {
            return Result.Success(ToDto(existing));
        }

        var charge = await gateway.ChargeAsync(command.OrderId, command.Amount, ct);
        if (!charge.IsSuccess)
        {
            // Decline: koi record save nahi hota
            return Result.Failure<PaymentDto>(PaymentErrors.Declined(charge.Reason ?? "unknown"));
        }

        var captured = PaymentRecord.Capture(command.OrderId, command.Amount, charge.TransactionId);
        if (captured.IsFailure)
        {
            return Result.Failure<PaymentDto>(captured.Error);
        }

        await repository.AddAsync(captured.Value, ct);

        if (!await uow.TrySaveChangesAsync(ct))
        {
            // Race: doosri request pehle save kar chuki hai, wahi record lo
            var winner = await repository.GetByOrderIdAsync(command.OrderId, ct);
            return winner is not null
                ? Result.Success(ToDto(winner))
                : Result.Failure<PaymentDto>(new Error("PAYMENT_CONCURRENCY_CONFLICT", "Dobara try karein."));
        }

        return Result.Success(ToDto(captured.Value));
    }

    /// <summary>Compensation: refund. Dobara call par wahi success (idempotent).</summary>
    public async Task<Result<PaymentDto>> RefundAsync(Guid orderId, CancellationToken ct = default)
    {
        var payment = await repository.GetByOrderIdAsync(orderId, ct);
        if (payment is null)
        {
            return Result.Failure<PaymentDto>(PaymentErrors.NotFound);
        }

        if (payment.Status == PaymentStatus.Refunded)
        {
            return Result.Success(ToDto(payment));
        }

        var refund = await gateway.RefundAsync(payment.TransactionId, ct);
        if (!refund.IsSuccess)
        {
            return Result.Failure<PaymentDto>(PaymentErrors.RefundFailed);
        }

        payment.MarkRefunded();

        return await uow.TrySaveChangesAsync(ct)
            ? Result.Success(ToDto(payment))
            : Result.Failure<PaymentDto>(new Error("PAYMENT_CONCURRENCY_CONFLICT", "Dobara try karein."));
    }

    public async Task<Result<PaymentDto>> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default)
    {
        var payment = await repository.GetByOrderIdAsync(orderId, ct);
        return payment is null
            ? Result.Failure<PaymentDto>(PaymentErrors.NotFound)
            : Result.Success(ToDto(payment));
    }

    private static PaymentDto ToDto(PaymentRecord p) =>
        new(p.Id, p.OrderId, p.Amount, p.Status.ToString(), p.TransactionId, p.CreatedAtUtc);
}

using Payment.Domain.Payments;

namespace Payment.Application.Abstractions;

public interface IPaymentRepository
{
    Task<PaymentRecord?> GetByOrderIdAsync(Guid orderId, CancellationToken ct);
    Task AddAsync(PaymentRecord payment, CancellationToken ct);
}

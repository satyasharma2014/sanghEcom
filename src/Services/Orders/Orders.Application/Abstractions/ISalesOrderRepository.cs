using Orders.Domain.Orders;

namespace Orders.Application.Abstractions;

public interface ISalesOrderRepository
{
    Task<SalesOrder?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(SalesOrder order, CancellationToken ct);
    Task<IReadOnlyList<SalesOrder>> GetRecoveryCandidatesAsync(int maxAttempts, int batchSize, CancellationToken ct);
}

public interface IOrdersUnitOfWork
{
    Task<bool> TrySaveChangesAsync(CancellationToken ct);
}

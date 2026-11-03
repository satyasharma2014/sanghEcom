using Microsoft.EntityFrameworkCore;
using Orders.Application.Abstractions;
using Orders.Domain.Orders;

namespace Orders.Infrastructure.Persistence;

public sealed class SalesOrderRepository(OrdersDbContext db) : ISalesOrderRepository
{
    public Task<SalesOrder?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.SalesOrders.Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task AddAsync(SalesOrder order, CancellationToken ct) =>
        await db.SalesOrders.AddAsync(order, ct);

    public async Task<IReadOnlyList<SalesOrder>> GetRecoveryCandidatesAsync(int maxAttempts, int batchSize, CancellationToken ct) =>
    await db.SalesOrders
        .Include(o => o.Lines)
        .Where(o => o.CompensationAttempts < maxAttempts
                 && o.Status == OrderStatus.CompensationPending)
        .OrderBy(o => o.CreatedAtUtc)
        .Take(batchSize)
        .ToListAsync(ct);
}

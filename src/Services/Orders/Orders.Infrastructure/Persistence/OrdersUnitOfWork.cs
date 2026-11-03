using Microsoft.EntityFrameworkCore;
using Orders.Application.Abstractions;

namespace Orders.Infrastructure.Persistence;

public sealed class OrdersUnitOfWork(OrdersDbContext db) : IOrdersUnitOfWork
{
    public async Task<bool> TrySaveChangesAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException)
        {
            return false;
        }
    }
}

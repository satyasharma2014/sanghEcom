using Microsoft.EntityFrameworkCore;
using Payment.Application.Abstractions;

namespace Payment.Infrastructure.Persistence;

public sealed class UnitOfWork(PaymentDbContext db) : IUnitOfWork
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
            // Unique index violation: same order ki doosri request pehle save kar chuki
            return false;
        }
    }
}

using Catalog.Application.Abstractions;

namespace Catalog.Infrastructure.Persistence;

public sealed class UnitOfWork(CatalogDbContext db) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}

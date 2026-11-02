namespace Catalog.Application.Abstractions;

/// <summary>
/// Saare changes ek saath database mein save karta hai.
/// </summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken ct);
}

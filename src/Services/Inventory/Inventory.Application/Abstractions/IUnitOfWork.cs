namespace Inventory.Application.Abstractions;

public interface IUnitOfWork
{
    /// <summary>False: concurrency ya duplicate conflict.</summary>
    Task<bool> TrySaveChangesAsync(CancellationToken ct);
}

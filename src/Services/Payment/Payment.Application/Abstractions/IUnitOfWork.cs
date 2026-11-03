namespace Payment.Application.Abstractions;

public interface IUnitOfWork
{
    /// <summary>False: unique ya concurrency conflict.</summary>
    Task<bool> TrySaveChangesAsync(CancellationToken ct);
}

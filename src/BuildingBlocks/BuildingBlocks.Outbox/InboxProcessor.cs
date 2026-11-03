using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BuildingBlocks.Outbox;

public sealed class InboxProcessor<TContext>(TContext db) where TContext : DbContext
{
    /// <returns>true: naya process hua, false: duplicate tha, skip.</returns>
    public async Task<bool> ProcessOnceAsync(
        string messageId,
        string handler,
        Func<CancellationToken, Task> work,
        CancellationToken ct)
    {
        if (await db.Set<InboxMessage>().AnyAsync(x => x.MessageId == messageId && x.Handler == handler, ct))
        {
            return false;
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            db.Set<InboxMessage>().Add(new InboxMessage(messageId, handler));
            await work(ct);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return true;
        }
        catch (DbUpdateException)
        {
            // Race: doosre consumer ne pehle insert kar diya
            return false;
        }
    }
}

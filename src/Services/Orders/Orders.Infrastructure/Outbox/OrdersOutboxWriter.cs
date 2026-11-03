using BuildingBlocks.Outbox;
using Orders.Application.Abstractions;
using Orders.Infrastructure.Persistence;

namespace Orders.Infrastructure.Outbox;

public sealed class OrdersOutboxWriter(OrdersDbContext db) : IOutboxWriter
{
    public void Add<T>(string routingKey, T payload) =>
        db.OutboxMessages.Add(OutboxMessage.Create(routingKey, payload));
}

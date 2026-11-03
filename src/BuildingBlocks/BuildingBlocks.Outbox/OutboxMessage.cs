using System.Text.Json;

namespace BuildingBlocks.Outbox;

public sealed class OutboxMessage
{
    private OutboxMessage() { }

    private OutboxMessage(string routingKey, string payload)
    {
        Id = Guid.NewGuid();
        OccurredOnUtc = DateTime.UtcNow;
        RoutingKey = routingKey;
        Payload = payload;
    }

    public Guid Id { get; private set; }
    public DateTime OccurredOnUtc { get; private set; }
    public string RoutingKey { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public DateTime? ProcessedOnUtc { get; private set; }
    public int Attempts { get; private set; }
    public string? LastError { get; private set; }

    public static OutboxMessage Create<T>(string routingKey, T payload) =>
        new(routingKey, JsonSerializer.Serialize(payload));

    public void MarkProcessed()
    {
        ProcessedOnUtc = DateTime.UtcNow;
        LastError = null;
    }

    public void RecordFailure(string error)
    {
        Attempts++;
        LastError = error;
    }
}

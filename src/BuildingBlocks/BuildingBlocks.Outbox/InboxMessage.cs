namespace BuildingBlocks.Outbox;

public sealed class InboxMessage
{
    private InboxMessage() { }

    public InboxMessage(string messageId, string handler)
    {
        MessageId = messageId;
        Handler = handler;
        ProcessedOnUtc = DateTime.UtcNow;
    }

    public string MessageId { get; private set; } = string.Empty;
    public string Handler { get; private set; } = string.Empty;
    public DateTime ProcessedOnUtc { get; private set; }
}

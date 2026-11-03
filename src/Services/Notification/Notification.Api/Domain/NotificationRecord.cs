namespace Notification.Api.Domain;

public sealed class NotificationRecord
{
    private NotificationRecord() { }

    public NotificationRecord(Guid orderId, string recipient, string subject)
    {
        Id = Guid.NewGuid();
        OrderId = orderId;
        Recipient = recipient;
        Subject = subject;
        SentAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public string Recipient { get; private set; } = string.Empty;
    public string Subject { get; private set; } = string.Empty;
    public DateTime SentAtUtc { get; private set; }
}

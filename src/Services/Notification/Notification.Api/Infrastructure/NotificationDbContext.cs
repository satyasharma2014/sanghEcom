using BuildingBlocks.Outbox;
using Microsoft.EntityFrameworkCore;
using Notification.Api.Domain;


namespace Notification.Api.Infrastructure;

public sealed class NotificationDbContext(DbContextOptions<NotificationDbContext> options) : DbContext(options)
{
    public DbSet<NotificationRecord> NotificationRecords => Set<NotificationRecord>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotificationRecord>(b =>
        {
            b.ToTable("notification_records");
            b.HasKey(x => x.Id);
            b.Property(x => x.Recipient).HasMaxLength(200).IsRequired();
            b.Property(x => x.Subject).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<OutboxMessage>(b =>
        {
            b.ToTable("outbox_messages");
            b.HasKey(x => x.Id);
            b.Property(x => x.RoutingKey).HasMaxLength(200).IsRequired();
            b.Property(x => x.Payload).IsRequired();
            b.HasIndex(x => x.OccurredOnUtc).HasFilter("\"ProcessedOnUtc\" IS NULL");
        });

        modelBuilder.Entity<InboxMessage>(b =>
        {
            b.ToTable("inbox_messages");
            b.HasKey(x => new { x.MessageId, x.Handler });
            b.Property(x => x.MessageId).HasMaxLength(100).IsRequired();
            b.Property(x => x.Handler).HasMaxLength(200).IsRequired();
        });
    }
}

using BuildingBlocks.Outbox;
using Microsoft.EntityFrameworkCore;
using Orders.Domain.Orders;

namespace Orders.Infrastructure.Persistence;

public sealed class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options)
{
    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SalesOrder>(b =>
        {
            b.ToTable("orders");
            b.HasKey(o => o.Id);
            b.Property(o => o.CustomerEmail).HasMaxLength(200).IsRequired();
            b.Property(o => o.TotalAmount).HasPrecision(18, 2);
            b.Property(o => o.Status).HasConversion<string>().HasMaxLength(30);
            b.Property(o => o.FailureReason).HasMaxLength(200);
            b.Property(o => o.CreatedAtUtc).IsRequired();

            b.OwnsMany(o => o.Lines, line =>
            {
                line.ToTable("order_lines");
                line.WithOwner().HasForeignKey("OrderId");
                line.HasKey("OrderId", "Sku");
                line.Property(l => l.Sku).HasMaxLength(50);
                line.Property(l => l.UnitPrice).HasPrecision(18, 2);
            });

            b.Navigation(o => o.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<OutboxMessage>(b =>
        {
            b.ToTable("outbox_messages");
            b.HasKey(x => x.Id);
            b.Property(x => x.RoutingKey).HasMaxLength(200).IsRequired();
            b.Property(x => x.Payload).IsRequired();
            b.Property(x => x.LastError).HasMaxLength(2000);
        });
    }
}

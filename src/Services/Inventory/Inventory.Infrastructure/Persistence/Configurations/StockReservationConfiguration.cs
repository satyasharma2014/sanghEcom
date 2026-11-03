using Inventory.Domain.Stock;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

public sealed class StockReservationConfiguration : IEntityTypeConfiguration<StockReservation>
{
    public void Configure(EntityTypeBuilder<StockReservation> builder)
    {
        builder.ToTable("stock_reservations");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Sku).HasMaxLength(50).IsRequired();
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);

        // Ek order mein ek SKU ka ek hi reservation
        builder.HasIndex(r => new { r.OrderId, r.Sku }).IsUnique();
    }
}

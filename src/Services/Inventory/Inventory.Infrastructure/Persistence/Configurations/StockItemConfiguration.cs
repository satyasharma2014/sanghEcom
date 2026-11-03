//using Inventory.Domain.Stock;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.Metadata.Builders;
//using Npgsql.EntityFrameworkCore.PostgreSQL;

//namespace Inventory.Infrastructure.Persistence.Configurations;

//public sealed class StockItemConfiguration : IEntityTypeConfiguration<StockItem>
//{
//    public void Configure(EntityTypeBuilder<StockItem> builder)
//    {
//        builder.ToTable("stock_items");
//        builder.HasKey(s => s.Id);
//        builder.Property(s => s.Sku).HasMaxLength(50).IsRequired();
//        builder.HasIndex(s => s.Sku).IsUnique();
//        builder.Property(s => s.AvailableQuantity).IsRequired();

//        // Optimistic concurrency: Postgres ka xmin column version ka kaam karta hai
//        builder.UseXminAsConcurrencyToken();
//    }
//}
using Inventory.Domain.Stock;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

public sealed class StockItemConfiguration : IEntityTypeConfiguration<StockItem>
{
    public void Configure(EntityTypeBuilder<StockItem> builder)
    {
        builder.ToTable("stock_items");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Sku).HasMaxLength(50).IsRequired();
        builder.HasIndex(s => s.Sku).IsUnique();
        builder.Property(s => s.AvailableQuantity).IsRequired();

        // Postgres ka xmin system column concurrency token ke roop mein use hoga
        builder.Property<uint>("xmin")
               .HasColumnName("xmin")
               .HasColumnType("xid")
               .ValueGeneratedOnAddOrUpdate()
               .IsConcurrencyToken();
    }
}

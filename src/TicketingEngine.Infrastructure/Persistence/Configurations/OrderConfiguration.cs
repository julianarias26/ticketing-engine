using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketingEngine.Domain.Entities;
using TicketingEngine.Domain.ValueObjects;

namespace TicketingEngine.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> b)
    {
        b.ToTable("orders");
        b.HasKey(o => o.Id);
        b.Property(o => o.Id).HasColumnName("id");
        b.Property(o => o.UserId).HasColumnName("user_id").IsRequired();
        b.Property(o => o.Status).HasColumnName("status").HasConversion<string>()
            .HasMaxLength(20).HasDefaultValue(OrderStatus.Pending);
        b.Property(o => o.TotalAmount).HasColumnName("total_amount")
            .HasPrecision(10, 2);
        b.Property(o => o.IdempotencyKey).HasColumnName("idempotency_key")
            .HasMaxLength(128).IsRequired();
        b.Property(o => o.ExpiresAt).HasColumnName("expires_at");
        b.Property(o => o.CreatedAt).HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        b.HasIndex(o => o.IdempotencyKey).IsUnique()
            .HasDatabaseName("ix_orders_idempotency_key");
        b.HasIndex(o => new { o.UserId, o.Status })
            .HasDatabaseName("ix_orders_user_status");
        b.HasIndex(o => new { o.ExpiresAt, o.Status })
            .HasDatabaseName("ix_orders_expires_status");

        b.HasMany(o => o.Items).WithOne()
            .HasForeignKey("OrderId").OnDelete(DeleteBehavior.Cascade);
    }
}

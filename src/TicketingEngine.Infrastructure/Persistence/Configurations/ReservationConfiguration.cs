using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketingEngine.Domain.Entities;
using TicketingEngine.Domain.ValueObjects;

namespace TicketingEngine.Infrastructure.Persistence.Configurations;

public sealed class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> b)
    {
        b.ToTable("reservations");
        b.HasKey(r => r.Id);
        b.Property(r => r.Id).HasColumnName("id");
        b.Property(r => r.SeatId).HasColumnName("seat_id").IsRequired();
        b.Property(r => r.UserId).HasColumnName("user_id").IsRequired();
        b.Property(r => r.OrderId).HasColumnName("order_id").IsRequired();
        b.Property(r => r.EventId).HasColumnName("event_id").IsRequired();
        b.Property(r => r.Status).HasColumnName("status").HasConversion<string>()
            .HasMaxLength(20).HasDefaultValue(ReservationStatus.Pending);
        b.Property(r => r.ExpiresAt).HasColumnName("expires_at");
        b.Property(r => r.CreatedAt).HasColumnName("reserved_at")
            .HasDefaultValueSql("NOW()");

        b.HasIndex(r => new { r.SeatId, r.Status })
            .HasDatabaseName("ix_reservations_seat_status");
        b.HasIndex(r => new { r.ExpiresAt, r.Status })
            .HasDatabaseName("ix_reservations_expires_status");
    }
}

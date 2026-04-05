using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketingEngine.Domain.Entities;
using TicketingEngine.Domain.ValueObjects;

namespace TicketingEngine.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users");
        b.HasKey(u => u.Id);
        b.Property(u => u.Email).HasMaxLength(256).IsRequired();
        b.Property(u => u.FullName).HasMaxLength(200).IsRequired();
        b.Property(u => u.PasswordHash).HasMaxLength(512).IsRequired();
        b.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
        b.Property(u => u.CreatedAt).HasDefaultValueSql("NOW()");
        b.HasIndex(u => u.Email).IsUnique().HasDatabaseName("ix_users_email");
    }
}

public sealed class VenueConfiguration : IEntityTypeConfiguration<Venue>
{
    public void Configure(EntityTypeBuilder<Venue> b)
    {
        b.ToTable("venues");
        b.HasKey(v => v.Id);
        b.Property(v => v.Name).HasMaxLength(200).IsRequired();
        b.Property(v => v.Address).HasMaxLength(500).IsRequired();
        b.Property(v => v.City).HasMaxLength(100).IsRequired();
        b.Property(v => v.CreatedAt).HasDefaultValueSql("NOW()");
        b.HasMany(v => v.Sections).WithOne(s => s.Venue)
            .HasForeignKey(s => s.VenueId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class SectionConfiguration : IEntityTypeConfiguration<Section>
{
    public void Configure(EntityTypeBuilder<Section> b)
    {
        b.ToTable("sections");
        b.HasKey(s => s.Id);
        b.Property(s => s.Name).HasMaxLength(100).IsRequired();
        b.Property(s => s.BasePrice).HasPrecision(10, 2);
        b.HasIndex(s => s.VenueId).HasDatabaseName("ix_sections_venue_id");
        b.HasMany(s => s.Seats).WithOne(seat => seat.Section)
            .HasForeignKey(seat => seat.SectionId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> b)
    {
        b.ToTable("events");
        b.HasKey(e => e.Id);
        b.Property(e => e.Name).HasMaxLength(300).IsRequired();
        b.Property(e => e.Description).HasMaxLength(2000);
        b.Property(e => e.Status).HasConversion<string>().HasMaxLength(20)
            .HasDefaultValue(EventStatus.Draft);
        b.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
        b.HasIndex(e => new { e.Status, e.EventDate })
            .HasDatabaseName("ix_events_status_date");
    }
}

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> b)
    {
        b.ToTable("order_items");
        b.HasKey(i => i.Id);
        b.Property(i => i.UnitPrice).HasPrecision(10, 2);
        b.HasIndex(i => new { i.SeatId, i.EventId }).IsUnique()
            .HasDatabaseName("ix_order_items_seat_event");
    }
}

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> b)
    {
        b.ToTable("payments");
        b.HasKey(p => p.Id);
        b.Property(p => p.Provider).HasMaxLength(50).IsRequired();
        b.Property(p => p.ProviderTransactionId).HasMaxLength(256).IsRequired();
        b.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(p => p.Amount).HasPrecision(10, 2);
        b.HasIndex(p => p.ProviderTransactionId).IsUnique()
            .HasDatabaseName("ix_payments_provider_tx_id");
    }
}

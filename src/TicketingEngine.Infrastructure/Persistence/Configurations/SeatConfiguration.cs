using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketingEngine.Domain.Entities;
using TicketingEngine.Domain.ValueObjects;

namespace TicketingEngine.Infrastructure.Persistence.Configurations;

public sealed class SeatConfiguration : IEntityTypeConfiguration<Seat>
{
    public void Configure(EntityTypeBuilder<Seat> b)
    {
        b.ToTable("seats");
        b.HasKey(s => s.Id);
        b.Property(s => s.Id).HasColumnName("id");
        b.Property(s => s.SectionId).HasColumnName("section_id").IsRequired();
        b.Property(s => s.RowLabel).HasColumnName("row_label").HasMaxLength(5).IsRequired();
        b.Property(s => s.SeatNumber).HasColumnName("seat_number").IsRequired();
        b.Property(s => s.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(SeatStatus.Available);
        b.Property(s => s.Version)
            .HasColumnName("version")
            .IsConcurrencyToken()
            .HasDefaultValue(0);

        b.HasIndex(s => new { s.SectionId, s.Status })
            .HasDatabaseName("ix_seats_section_status");
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TicketingEngine.Infrastructure.Persistence.Configurations
{
    public class SeatConfiguration : IEntityTypeConfiguration<Seat>
    {
        public void Configure(EntityTypeBuilder<Seat> builder)
        {
            builder.ToTable("seats");
            builder.HasKey(s => s.Id);

            // Optimistic Concurrency Token — EF Core lanza
            // DbUpdateConcurrencyException si version cambió desde la lectura
            builder.Property(s => s.Version)
                .IsConcurrencyToken()
                .HasColumnName("version");

            builder.Property(s => s.Status)
                .HasConversion<string>()
                .HasMaxLength(20);
        }
    }
}

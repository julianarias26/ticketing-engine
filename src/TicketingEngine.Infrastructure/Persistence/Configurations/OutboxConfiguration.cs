using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketingEngine.Domain.Entities;

namespace TicketingEngine.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> b)
    {
        b.ToTable("outbox_messages");
        b.HasKey(m => m.Id);
        b.Property(m => m.Id).HasColumnName("id");
        b.Property(m => m.AggregateType).HasColumnName("aggregate_type").HasMaxLength(100).IsRequired();
        b.Property(m => m.EventType).HasColumnName("event_type").HasMaxLength(200).IsRequired();
        b.Property(m => m.Payload).HasColumnName("payload").IsRequired();
        b.Property(m => m.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
        b.Property(m => m.ProcessedAt).HasColumnName("processed_at");
        b.Property(m => m.RetryCount).HasColumnName("retry_count").HasDefaultValue(0);

        // Partial index — only unprocessed messages (low cardinality in steady state)
        b.HasIndex(m => new { m.CreatedAt, m.RetryCount })
            .HasFilter("processed_at IS NULL")
            .HasDatabaseName("ix_outbox_unprocessed");
    }
}

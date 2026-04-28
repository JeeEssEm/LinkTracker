using LinkTracker.Scrapper.Infrastructure.Orm.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinkTracker.Scrapper.Infrastructure.Orm.Context.Configurations;

public class OutboxEventConfiguration : IEntityTypeConfiguration<OutboxEvent>
{
    public void Configure(EntityTypeBuilder<OutboxEvent> builder)
    {
        builder.ToTable("outbox_events");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id);

        builder.Property(e => e.Headers)
            .HasColumnType("jsonb");

        builder.Property(e => e.Payload)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(e => e.ErrorMessages)
            .HasColumnType("jsonb");

        builder.Property(e => e.Status)
            .HasConversion<int>();

        builder.Property(e => e.RetryCount)
            .HasConversion<int>();

        builder.Property(e => e.EventId)
            .HasConversion<int>();

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("idx_outbox_worker")
            .HasFilter("status IN (0, 1, 3)");
    }
}
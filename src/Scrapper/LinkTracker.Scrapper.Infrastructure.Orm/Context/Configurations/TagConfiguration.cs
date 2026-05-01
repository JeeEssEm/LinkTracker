using LinkTracker.Scrapper.Infrastructure.Orm.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinkTracker.Scrapper.Infrastructure.Orm.Context.Configurations;

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).UseIdentityColumn();
        builder.Property(e => e.Name).HasMaxLength(64).IsRequired();

        builder.HasOne(e => e.Subscription)
            .WithMany(s => s.Tags)
            .HasForeignKey(e => new { e.ChatId, e.LinkId })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Link)
            .WithMany(l => l.Tags)
            .HasForeignKey(e => e.LinkId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
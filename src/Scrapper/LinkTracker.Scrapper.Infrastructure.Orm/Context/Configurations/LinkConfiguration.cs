using LinkTracker.Scrapper.Infrastructure.Orm.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinkTracker.Scrapper.Infrastructure.Orm.Context.Configurations;

public class LinkConfiguration : IEntityTypeConfiguration<Link>
{
    public void Configure(EntityTypeBuilder<Link> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Url)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(l => l.CheckerKey)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(l => l.LastCheckAt)
            .IsRequired(false);

        builder.Property(l => l.LastChangeAt)
            .IsRequired(false);

        builder.HasIndex(l => l.Url)
            .IsUnique();

        builder.HasIndex(l => l.LastCheckAt);

        builder.HasMany(l => l.Subscriptions)
            .WithOne(s => s.Link)
            .HasForeignKey(s => s.LinkId);

        builder.HasMany(l => l.LinkTags)
            .WithOne(lt => lt.Link)
            .HasForeignKey(lt => lt.LinkId);
    }
}

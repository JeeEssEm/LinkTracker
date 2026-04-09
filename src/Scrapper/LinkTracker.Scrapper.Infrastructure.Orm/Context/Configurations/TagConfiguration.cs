using LinkTracker.Scrapper.Infrastructure.Orm.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinkTracker.Scrapper.Infrastructure.Orm.Context.Configurations;

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(t => new { t.Name, t.ChatId })
            .IsUnique();

        builder.HasOne(t => t.Chat)
            .WithMany(c => c.Tags)
            .HasForeignKey(t => t.ChatId);

        builder.HasMany(t => t.LinkTags)
            .WithOne(lt => lt.Tag)
            .HasForeignKey(lt => lt.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
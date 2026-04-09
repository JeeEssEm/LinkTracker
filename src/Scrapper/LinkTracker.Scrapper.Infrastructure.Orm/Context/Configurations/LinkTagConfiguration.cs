using LinkTracker.Scrapper.Infrastructure.Orm.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinkTracker.Scrapper.Infrastructure.Orm.Context.Configurations;

public class LinkTagConfiguration : IEntityTypeConfiguration<LinkTag>
{
    public void Configure(EntityTypeBuilder<LinkTag> builder)
    {
        builder.HasKey(lt => new { lt.LinkId, lt.TagId });

        builder.HasOne(lt => lt.Link)
            .WithMany(l => l.LinkTags)
            .HasForeignKey(lt => lt.LinkId);
    }
}

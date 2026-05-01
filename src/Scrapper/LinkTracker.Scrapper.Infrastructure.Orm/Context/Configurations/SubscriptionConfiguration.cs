using LinkTracker.Scrapper.Infrastructure.Orm.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinkTracker.Scrapper.Infrastructure.Orm.Context.Configurations;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.HasKey(e => new { e.ChatId, e.LinkId });

        builder.HasOne(e => e.Chat)
            .WithMany(c => c.Subscriptions)
            .HasForeignKey(e => e.ChatId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Link)
            .WithMany(l => l.Subscriptions)
            .HasForeignKey(e => e.LinkId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

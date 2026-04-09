using LinkTracker.Scrapper.Infrastructure.Orm.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinkTracker.Scrapper.Infrastructure.Orm.Context.Configurations;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.HasKey(s => new { s.ChatId, s.LinkId });

        builder.HasOne(s => s.Chat)
            .WithMany(c => c.Subscriptions)
            .HasForeignKey(s => s.ChatId);

        builder.HasOne(s => s.Link)
            .WithMany(l => l.Subscriptions)
            .HasForeignKey(s => s.LinkId);
    }
}

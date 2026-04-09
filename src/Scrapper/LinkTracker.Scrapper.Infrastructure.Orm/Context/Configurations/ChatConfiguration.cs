using LinkTracker.Scrapper.Infrastructure.Orm.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinkTracker.Scrapper.Infrastructure.Orm.Context.Configurations;

public class ChatConfiguration : IEntityTypeConfiguration<Chat>
{
    public void Configure(EntityTypeBuilder<Chat> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.CreatedAt)
            .HasDefaultValue(DateTime.UtcNow);

        builder.HasMany(c => c.Subscriptions)
            .WithOne(s => s.Chat)
            .HasForeignKey(s => s.ChatId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Tags)
            .WithOne(t => t.Chat)
            .HasForeignKey(t => t.ChatId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
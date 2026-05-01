using LinkTracker.Scrapper.Infrastructure.Orm.Models.Sources.StackOverflow;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinkTracker.Scrapper.Infrastructure.Orm.Context.Configurations.Sources.StackOverflow;

public class StackOverflowQuestionConfiguration : IEntityTypeConfiguration<StackOverflowQuestion>
{
    public void Configure(EntityTypeBuilder<StackOverflowQuestion> builder)
    {
        builder.ToTable("stackoverflow_questions");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("(now() AT TIME ZONE 'utc')");

        builder.HasOne(e => e.Link)
            .WithOne(l => l.StackOverflowQuestion)
            .HasForeignKey<StackOverflowQuestion>(e => e.LinkId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
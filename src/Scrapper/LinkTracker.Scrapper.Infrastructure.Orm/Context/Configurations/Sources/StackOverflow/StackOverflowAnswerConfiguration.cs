using LinkTracker.Scrapper.Infrastructure.Orm.Models.Sources.StackOverflow;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinkTracker.Scrapper.Infrastructure.Orm.Context.Configurations.Sources.StackOverflow;

public class StackOverflowAnswerConfiguration : IEntityTypeConfiguration<StackOverflowAnswer>
{
    public void Configure(EntityTypeBuilder<StackOverflowAnswer> builder)
    {
        builder.ToTable("stackoverflow_answers");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("(now() AT TIME ZONE 'utc')");

        builder.HasOne(e => e.Question)
            .WithMany(q => q.Answers)
            .HasForeignKey(e => e.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
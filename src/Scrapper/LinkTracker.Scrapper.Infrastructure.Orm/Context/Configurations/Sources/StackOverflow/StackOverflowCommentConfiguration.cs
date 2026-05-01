using LinkTracker.Scrapper.Infrastructure.Orm.Models.Sources.StackOverflow;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinkTracker.Scrapper.Infrastructure.Orm.Context.Configurations.Sources.StackOverflow;

public class StackOverflowCommentConfiguration : IEntityTypeConfiguration<StackOverflowComment>
{
    public void Configure(EntityTypeBuilder<StackOverflowComment> builder)
    {
        builder.ToTable("stackoverflow_comments");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("(now() AT TIME ZONE 'utc')");

        builder.HasOne(e => e.Answer)
            .WithMany(a => a.Comments)
            .HasForeignKey(e => e.AnswerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
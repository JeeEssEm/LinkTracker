using LinkTracker.Scrapper.Infrastructure.Orm.Models.Sources.StackOverflow;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinkTracker.Scrapper.Infrastructure.Orm.Context.Configurations.Sources.StackOverflow;


public class StackOverflowQuestionCommentConfiguration : IEntityTypeConfiguration<StackOverflowQuestionComment>
{
    public void Configure(EntityTypeBuilder<StackOverflowQuestionComment> builder)
    {
        builder.ToTable("stackoverflow_question_comments");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.HasOne(e => e.Question)
            .WithMany(q => q.Comments)
            .HasForeignKey(e => e.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

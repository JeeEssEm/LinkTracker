using LinkTracker.Scrapper.Infrastructure.Orm.Models.Sources.Github;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinkTracker.Scrapper.Infrastructure.Orm.Context.Configurations.Sources.Github;

public class GithubIssueConfiguration : IEntityTypeConfiguration<GithubIssue>
{
    public void Configure(EntityTypeBuilder<GithubIssue> builder)
    {
        builder.HasKey(e => new { e.GitHubRepoId, e.Number });
        builder.Property(e => e.PreviousStatus).HasMaxLength(50).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();

        builder.HasOne(e => e.Repository)
            .WithMany(r => r.Issues)
            .HasForeignKey(e => e.GitHubRepoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
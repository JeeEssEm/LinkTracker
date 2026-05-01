using LinkTracker.Scrapper.Infrastructure.Orm.Models.Sources.Github;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinkTracker.Scrapper.Infrastructure.Orm.Context.Configurations.Sources.Github;

public class GithubPullRequestConfiguration : IEntityTypeConfiguration<GithubPullRequest>
{
    public void Configure(EntityTypeBuilder<GithubPullRequest> builder)
    {
        builder.HasKey(e => new { e.GithubRepoId, e.Number });
        builder.Property(e => e.PreviousState).HasMaxLength(50).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();

        builder.HasOne(e => e.Repository)
            .WithMany(r => r.PullRequests)
            .HasForeignKey(e => e.GithubRepoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
using LinkTracker.Scrapper.Infrastructure.Orm.Models.Sources.Github;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LinkTracker.Scrapper.Infrastructure.Orm.Context.Configurations.Sources.Github;

public class GithubRepositoryConfiguration : IEntityTypeConfiguration<GithubRepository>
{
    public void Configure(EntityTypeBuilder<GithubRepository> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).UseIdentityColumn();
        builder.Property(e => e.Name).HasMaxLength(255).IsRequired();
        builder.Property(e => e.Owner).HasMaxLength(255).IsRequired();

        builder.HasOne(e => e.Link)
            .WithOne(l => l.GithubRepository)
            .HasForeignKey<GithubRepository>(e => e.LinkId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

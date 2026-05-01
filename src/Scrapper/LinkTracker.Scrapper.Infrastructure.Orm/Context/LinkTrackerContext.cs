using LinkTracker.Scrapper.Infrastructure.Orm.Models;
using LinkTracker.Scrapper.Infrastructure.Orm.Models.Sources.Github;
using LinkTracker.Scrapper.Infrastructure.Orm.Models.Sources.StackOverflow;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;

namespace LinkTracker.Scrapper.Infrastructure.Orm.Context;

public class LinkTrackerContext : DbContext
{
    public DbSet<Chat> Chats { get; set; }
    public DbSet<Link> Links { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<OutboxEvent> OutboxEvents { get; set; }

    public DbSet<Subscription> Subscriptions { get; set; }

    public DbSet<StackOverflowQuestion> StackOverflowQuestions { get; set; }
    public DbSet<StackOverflowAnswer> StackOverflowAnswers { get; set; }
    public DbSet<StackOverflowComment> StackOverflowComments { get; set; }
    public DbSet<StackOverflowQuestionComment> StackOverflowQuestionComments { get; set; }

    public DbSet<GithubRepository> GithubRepositories { get; set; }
    public DbSet<GithubIssue> GithubIssues { get; set; }
    public DbSet<GithubPullRequest> GithubPullRequests { get; set; }

    public LinkTrackerContext(DbContextOptions<LinkTrackerContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LinkTrackerContext).Assembly);
    }
}
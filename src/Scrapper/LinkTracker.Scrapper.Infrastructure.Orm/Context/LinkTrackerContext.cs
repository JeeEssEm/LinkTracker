using LinkTracker.Scrapper.Infrastructure.Orm.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;

namespace LinkTracker.Scrapper.Infrastructure.Orm.Context;

public class LinkTrackerContext : DbContext
{
    public DbSet<Chat> Chats { get; set; }
    public DbSet<Link> Links { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<LinkTag> LinkTags { get; set; }
    public DbSet<OutboxEvent> OutboxEvents { get; set; }

    public DbSet<Subscription> Subscriptions { get; set; }

    public LinkTrackerContext(DbContextOptions<LinkTrackerContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LinkTrackerContext).Assembly);
    }
}
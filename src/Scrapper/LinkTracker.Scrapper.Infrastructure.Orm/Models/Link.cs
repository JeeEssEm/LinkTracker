using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LinkTracker.Scrapper.Infrastructure.Orm.Models.Sources.Github;
using LinkTracker.Scrapper.Infrastructure.Orm.Models.Sources.StackOverflow;
using Microsoft.EntityFrameworkCore;

namespace LinkTracker.Scrapper.Infrastructure.Orm.Models;


public class Link
{
    public Guid Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public short SourceType { get; set; }
    public DateTimeOffset? LastCheckAt { get; set; }

    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    public ICollection<Tag> Tags { get; set; } = new List<Tag>();

    public StackOverflowQuestion? StackOverflowQuestion { get; set; }
    public GithubRepository? GithubRepository { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace LinkTracker.Scrapper.Infrastructure.Orm.Models;

public class Chat
{
    public long Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    public ICollection<Tag> Tags { get; set; } = new List<Tag>();
}

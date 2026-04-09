using System.ComponentModel.DataAnnotations;

namespace LinkTracker.Scrapper.Infrastructure.Orm.Models;

public class Chat
{
    public long Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public ICollection<Subscription> Subscriptions { get; set; } = [];
    public ICollection<Tag> Tags { get; set; } = [];
}

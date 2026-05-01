using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LinkTracker.Scrapper.Infrastructure.Orm.Models;

public class Subscription
{
    public long ChatId { get; set; }
    public Guid LinkId { get; set; }

    public Chat Chat { get; set; } = null!;
    public Link Link { get; set; } = null!;
    public ICollection<Tag> Tags { get; set; } = new List<Tag>();
}

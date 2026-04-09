using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LinkTracker.Scrapper.Infrastructure.Orm.Models;

public class Subscription
{
    public long ChatId { get; set; }
    public long LinkId { get; set; }

    public Link Link { get; set; } = null!;
    public Chat Chat { get; set; } = null!;
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LinkTracker.Scrapper.Infrastructure.Orm.Models;


public class Tag
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public long ChatId { get; set; }
    public Guid LinkId { get; set; }

    public Subscription Subscription { get; set; } = null!;
    public Link Link { get; set; } = null!;
}

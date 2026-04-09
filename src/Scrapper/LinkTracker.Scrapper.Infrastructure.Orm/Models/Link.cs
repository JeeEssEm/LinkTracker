using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LinkTracker.Scrapper.Infrastructure.Orm.Models;


public class Link
{
    public long Id { get; set; }

    public string Url { get; set; }

    public string CheckerKey { get; set; }

    public DateTime? LastCheckAt { get; set; }
    public DateTime? LastChangeAt { get; set; }

    public ICollection<Subscription> Subscriptions { get; set; } = [];
    public ICollection<LinkTag> LinkTags { get; set; } = [];
}

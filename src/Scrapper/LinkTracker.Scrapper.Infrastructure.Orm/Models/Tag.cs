using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LinkTracker.Scrapper.Infrastructure.Orm.Models;


public class Tag
{
    public long Id { get; set; }

    public string Name { get; set; }

    public long ChatId { get; set; }

    public Chat Chat { get; set; } = null!;
    public ICollection<LinkTag> LinkTags { get; set; } = [];
}

using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LinkTracker.Scrapper.Infrastructure.Orm.Models;

public class LinkTag
{
    public long LinkId { get; set; }
    public long TagId { get; set; }

    public Link Link { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
}

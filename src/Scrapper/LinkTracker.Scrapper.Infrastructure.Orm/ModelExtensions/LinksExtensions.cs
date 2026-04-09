using LinkTracker.Scrapper.Infrastructure.Orm.Models;
using DomainTag = LinkTracker.Scrapper.Domain.Tag.Entities.Tag;
using DomainLink = LinkTracker.Scrapper.Domain.Links.Entities.Link;

namespace LinkTracker.Scrapper.Infrastructure.Orm.ModelExtensions;

public static class LinksExtensions
{
    internal static DomainLink ToDomain(this Link link)
        => new DomainLink(link.Id, link.Url, link.CheckerKey);

    internal static List<DomainLink> ToDomain(this IEnumerable<Link> links)
        => links.Select(lnk => lnk.ToDomain()).ToList();

}

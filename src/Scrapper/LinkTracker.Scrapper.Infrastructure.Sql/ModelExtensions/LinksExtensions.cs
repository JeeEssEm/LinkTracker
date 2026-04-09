using LinkTracker.Scrapper.Domain.Links.Entities;
using LinkTracker.Scrapper.Domain.Tag.Entities;

namespace LinkTracker.Scrapper.Infrastructure.Sql.ModelExtensions;

public static class LinksExtensions
{
    public static Link ToDomain(this Models.Link link) => new(link.Id, link.Url, link.CheckerKey);

    public static IEnumerable<Link> ToDomain(this IEnumerable<Models.Link> links) =>
        links.Select(ToDomain);
}

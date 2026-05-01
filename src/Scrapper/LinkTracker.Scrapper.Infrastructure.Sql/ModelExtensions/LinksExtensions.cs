using LinkTracker.Scrapper.Domain.Tracking.Entities;
using LinkTracker.Scrapper.Domain.Tracking.ValueObjects;

namespace LinkTracker.Scrapper.Infrastructure.Sql.ModelExtensions;

public static class LinksExtensions
{
    public static Link ToDomain(this Models.Link link)
    {
        if (!Enum.IsDefined(typeof(LinkSourceType), link.SourceType))
        {
            throw new ArgumentException($"Incorrect type of source_type. Received: {link.SourceType}");
        }

        var parseResult = Link.ParseUri(link.Url);
        if (parseResult.IsFailed)
        {
            throw new ArgumentException($"Cannot parse uri. Received: {link.Url}");
        }

        return new Link(new LinkId(link.Id), parseResult.Value, (LinkSourceType)link.SourceType, link.LastCheckAt);
    }
}

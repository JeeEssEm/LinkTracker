using LinkTracker.Scrapper.Domain.Tracking.Entities;
using LinkTracker.Scrapper.Domain.Tracking.Repositories;
using LinkTracker.Scrapper.Domain.Tracking.ValueObjects;
using LinkTracker.Scrapper.Infrastructure.Orm.Context;
using Microsoft.EntityFrameworkCore;
using Link = LinkTracker.Scrapper.Domain.Tracking.Entities.Link;

namespace LinkTracker.Scrapper.Infrastructure.Orm.Repositories;

public class LinkRepository(LinkTrackerContext dbContext) : ILinkRepository
{
    public async Task<Link> GetOrCreateAsync(Uri uri, LinkSourceType sourceType)
    {
        var existedLink = await dbContext.Links.FirstOrDefaultAsync(l => l.Url == uri.AbsoluteUri);
        if (existedLink == null)
        {
            var link = new Models.Link()
            {
                Url = uri.AbsoluteUri,
                SourceType = (short)sourceType
            };
            dbContext.Add(link);
            await dbContext.SaveChangesAsync();
            return new Link(new LinkId(link.Id), uri, sourceType, null);
        }

        return new Link(new LinkId(existedLink.Id), uri, sourceType, existedLink.LastCheckAt);
    }

    public async Task<bool> RemoveLinkAsync(Uri uri)
    {
        var link = await dbContext.Links.FirstOrDefaultAsync(l => l.Url == uri.AbsoluteUri);
        if (link == null)
        {
            return false;
        }

        dbContext.Links.Remove(link);
        await dbContext.SaveChangesAsync();
        return true;
    }
}

using LinkTracker.Scrapper.Domain.Tracking.Entities;
using LinkTracker.Scrapper.Domain.Tracking.ValueObjects;

namespace LinkTracker.Scrapper.Domain.Tracking.Repositories;

public interface ILinkRepository
{
    Task<Link> GetOrCreateAsync(Uri uri, LinkSourceType sourceType);
    Task<bool> RemoveLinkAsync(Uri uri);
}

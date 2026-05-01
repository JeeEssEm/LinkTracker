using LinkTracker.Scrapper.Domain.Tracking.Entities;

namespace LinkTracker.Scrapper.Domain.Tracking.SourceHandlers.Github;

public interface IGithubRepository
{
    Task CreateAsync(LinkId linkId, string name, string ownerName);
}

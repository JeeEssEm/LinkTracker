using LinkTracker.Scrapper.Application.Scheduling;
using LinkTracker.Scrapper.Domain.Tracking.ValueObjects;

namespace LinkTracker.Scrapper.Application.Repositories;

public interface ILinkBulkRepository
{
    Task RemoveLinksAsync(IEnumerable<Guid> linkIds);
    Task<List<LinkCheckEntry>> GetLinksToCheck(int page, int batchSize, DateTime now);
    Task UpdateCheckDatesAt(IEnumerable<Guid> linkIds);
    Task<Dictionary<LinkSourceType, long>> GetNumberOfTrackedLinks();
}

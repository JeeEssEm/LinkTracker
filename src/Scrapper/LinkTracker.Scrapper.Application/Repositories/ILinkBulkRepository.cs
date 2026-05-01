using LinkTracker.Scrapper.Application.Scheduling;

namespace LinkTracker.Scrapper.Application.Repositories;

public interface ILinkBulkRepository
{
    Task RemoveLinksAsync(IEnumerable<Guid> linkIds);
    Task<List<LinkCheckEntry>> GetLinksToCheck(int page, int batchSize, DateTime now);
    Task UpdateCheckDatesAt(IEnumerable<Guid> linkIds);
}

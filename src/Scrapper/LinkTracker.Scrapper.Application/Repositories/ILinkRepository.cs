using FluentResults;
using LinkTracker.Scrapper.Application.Scheduling;
using LinkTracker.Scrapper.Domain;
using LinkTracker.Scrapper.Domain.Chats.Entities;
using LinkTracker.Scrapper.Domain.Links.Entities;

namespace LinkTracker.Scrapper.Application.Repositories;

public interface ILinkRepository
{
    Task<Link?> GetLinkAsync(string url);
    Task<Link> CreateLink(string url, string checkerKey);
    Task<Link?> RemoveLink(string url);
    Task UpdateCheckDatesAt(long[] linkIds);
    Task<List<LinkCheckEntry>> GetLinksToCheck(int page, int limit, DateTime currentTime);
    Task RemoveLinksAsync(IEnumerable<long> linkIds);
}

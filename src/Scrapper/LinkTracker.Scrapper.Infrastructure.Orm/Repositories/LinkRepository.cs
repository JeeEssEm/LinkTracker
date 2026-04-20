using FluentResults;
using LinkTracker.Scrapper.Application.Repositories;
using LinkTracker.Scrapper.Application.Scheduling;
using LinkTracker.Scrapper.Errors.Chats;
using LinkTracker.Scrapper.Errors.Links;
using LinkTracker.Scrapper.Infrastructure.Orm.Context;
using LinkTracker.Scrapper.Infrastructure.Orm.ModelExtensions;
using LinkTracker.Scrapper.Infrastructure.Orm.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Link = LinkTracker.Scrapper.Domain.Links.Entities.Link;

namespace LinkTracker.Scrapper.Infrastructure.Orm.Repositories;

public class LinkRepository(LinkTrackerContext dbContext, ILogger<LinkRepository> logger) : ILinkRepository
{
    public async Task<Link?> GetLinkAsync(string url)
    {
        var linkModel = await dbContext.Links
            .Where(link => link.Url == url)
            .FirstOrDefaultAsync();

        return linkModel?.ToDomain();
    }

    public async Task<Link> CreateLink(string url, string checkerKey)
    {
        var link = new Models.Link { Url = url, CheckerKey = checkerKey };

        await dbContext.AddAsync(link);
        await dbContext.SaveChangesAsync();

        return link.ToDomain();
    }

    public async Task<Link?> RemoveLink(string url)
    {
        var link = await dbContext.Links
            .FirstOrDefaultAsync(link => link.Url == url);

        if (link == null)
        {
            return null;
        }

        dbContext.Links.Remove(link);
        await dbContext.SaveChangesAsync();

        return link.ToDomain();
    }

    public async Task UpdateCheckDatesAt(long[] linkIds)
    {
        await dbContext.Links
            .Where(l => linkIds.Contains(l.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(l => l.LastCheckAt, DateTime.UtcNow));

        await dbContext.SaveChangesAsync();
    }

    public async Task<List<LinkCheckEntry>> GetLinksToCheck(int page, int limit, DateTime currentTime)
    {
        var links = await dbContext.Links
            .Where(lnk => lnk.LastCheckAt < currentTime || lnk.LastCheckAt == null)
            .OrderBy(lnk => lnk.LastCheckAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(lnk => new { Link = lnk, SubscriptionChatIds = lnk.Subscriptions.Select(sub => sub.ChatId) })
            .ToListAsync();

        return links.Select(e => new LinkCheckEntry(
                e.Link.Id, e.Link.Url, e.Link.CheckerKey, e.Link.LastCheckAt,
                e.SubscriptionChatIds.ToList()
            )
        ).ToList();
    }

    public async Task RemoveLinksAsync(IEnumerable<long> linkIds)
    {
        await dbContext.Links.Where(l => linkIds.Contains(l.Id)).ExecuteDeleteAsync();
    }
}
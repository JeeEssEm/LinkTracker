using LinkTracker.Scrapper.Application.ReadModels;
using LinkTracker.Scrapper.Application.Repositories;
using LinkTracker.Scrapper.Infrastructure.Orm.Context;
using LinkTracker.Scrapper.Infrastructure.Orm.Models;
using Microsoft.EntityFrameworkCore;

namespace LinkTracker.Scrapper.Infrastructure.Orm.Repositories;

public class SubscriptionRepository(LinkTrackerContext dbContext) : ISubscriptionRepository
{
    public Task<bool> SubscriptionExistsAsync(long chatId, long linkId)
    {
        return dbContext.Subscriptions.AnyAsync(sub => sub.ChatId == chatId && sub.LinkId == linkId);
    }

    public Task<bool> SubscriptionExistsAsync(long chatId, string url)
    {
        return dbContext.Subscriptions.AnyAsync(sub => sub.ChatId == chatId && sub.Link.Url == url);
    }

    public async Task CreateSubscriptionAsync(long chatId, long linkId)
    {
        var sub = new Subscription { ChatId = chatId, LinkId = linkId };

        await dbContext.AddAsync(sub);
        await dbContext.SaveChangesAsync();
    }

    public async Task RemoveSubscriptionAsync(long chatId, string url)
    {
        await dbContext.Subscriptions.Where(sub => sub.ChatId == chatId && sub.Link.Url == url).ExecuteDeleteAsync();
    }

    public async Task<SubscribedLink?> GetSubscriptionAsync(long chatId, long linkId)
    {
        var link = await dbContext.Links
            .Where(link => link.Id == linkId && link.Subscriptions.Any(sub => sub.ChatId == chatId))
            .Include(link => link.LinkTags
                .Where(lt => lt.Tag.ChatId == chatId)
            )
            .ThenInclude(linkTag => linkTag.Tag)
            .FirstOrDefaultAsync();
        if (link == null)
        {
            return null;
        }

        return new SubscribedLink(linkId, link.Url, link.LinkTags.Select(lt => lt.Tag.Name).ToList());
    }

    public async Task<SubscribedLink?> GetSubscriptionAsync(long chatId, string url)
    {
        var link = await dbContext.Links
            .Where(link => link.Url == url && link.Subscriptions.Any(sub => sub.ChatId == chatId))
            .Include(link => link.LinkTags
                .Where(lt => lt.Tag.ChatId == chatId)
            )
            .ThenInclude(linkTag => linkTag.Tag)
            .FirstOrDefaultAsync();

        if (link == null)
        {
            return null;
        }

        return new SubscribedLink(link.Id, link.Url, link.LinkTags.Select(lt => lt.Tag.Name).ToList());
    }

    public async Task<List<SubscribedLink>> GetSubscriptionsByChatAsync(long chatId)
    {
        var links = await dbContext.Links
            .Where(link => link.Subscriptions.Any(sub => sub.ChatId == chatId))
            .Include(link => link.LinkTags
                .Where(lt => lt.Tag.ChatId == chatId)
            )
            .ThenInclude(linkTag => linkTag.Tag)
            .ToListAsync();

        return links
            .Select(lnk => new SubscribedLink(
                lnk.Id, lnk.Url,
                lnk.LinkTags.Select(lt => lt.Tag.Name).ToList())
        ).ToList();
    }
}
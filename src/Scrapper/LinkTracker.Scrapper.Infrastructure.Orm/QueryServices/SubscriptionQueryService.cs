using LinkTracker.Scrapper.Application.QueryServices;
using LinkTracker.Scrapper.Contracts.Dtos.Links;
using LinkTracker.Scrapper.Domain.Tracking.Entities;
using LinkTracker.Scrapper.Domain.Tracking.ValueObjects;
using LinkTracker.Scrapper.Infrastructure.Orm.Context;
using Microsoft.EntityFrameworkCore;

namespace LinkTracker.Scrapper.Infrastructure.Orm.QueryServices;

public class SubscriptionQueryService(LinkTrackerContext dbContext) : ISubscriptionQueryService
{
    public async Task<IReadOnlyList<LinkResponse>> ListSubscriptionsByChatAsync(ChatId chatId)
    {
        var subs = await dbContext.Subscriptions
            .Include(s => s.Link)
            .Include(s => s.Tags)
            .Where(s => s.ChatId == chatId.Value)
            .ToListAsync();

        return subs
            .Select(s => new LinkResponse(s.LinkId, s.Link.Url, s.Tags.Select(t => t.Name).ToList()))
            .ToList();
    }
}
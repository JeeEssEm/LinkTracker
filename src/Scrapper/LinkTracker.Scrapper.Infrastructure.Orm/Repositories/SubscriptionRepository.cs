using LinkTracker.Scrapper.Domain.Tracking.Entities;
using LinkTracker.Scrapper.Domain.Tracking.Repositories;
using LinkTracker.Scrapper.Domain.Tracking.ValueObjects;
using LinkTracker.Scrapper.Infrastructure.Orm.Context;
using Microsoft.EntityFrameworkCore;
using Subscription = LinkTracker.Scrapper.Domain.Tracking.Entities.Subscription;

namespace LinkTracker.Scrapper.Infrastructure.Orm.Repositories;

public class SubscriptionRepository(LinkTrackerContext dbContext) : ISubscriptionRepository
{
    public async Task<bool> CreateAsync(Subscription subscription)
    {
        if (await dbContext.Subscriptions.CountAsync(
                s =>
                s.ChatId == subscription.Id.ChatId.Value &&
                s.LinkId == subscription.Id.LinkId.Value) != 0)
        {
            return false;
        }

        var subModel = new Models.Subscription()
        {
            ChatId = subscription.Id.ChatId.Value,
            LinkId = subscription.Id.LinkId.Value,
            Tags = subscription.Tags.Select(t => new Models.Tag() { Name = t.Name }).ToList()
        };
        dbContext.Subscriptions.Add(subModel);
        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<Subscription?> GetAsync(ChatId chatId, string url)
    {
        var sub = await dbContext.Subscriptions
            .Include(s => s.Tags)
            .FirstOrDefaultAsync(s => s.ChatId == chatId.Value && s.Link.Url == url);
        if (sub == null)
        {
            return null;
        }

        return new Subscription(
            new SubscriptionId(new LinkId(sub.LinkId), new ChatId(sub.ChatId)),
            sub.Tags.Select(t => new Tag(t.Name))
            );
    }

    public async Task RemoveAsync(Subscription subscription)
    {
        await dbContext.Subscriptions.Where(sub =>
            sub.ChatId == subscription.Id.ChatId.Value &&
            sub.LinkId == subscription.Id.LinkId.Value
        ).ExecuteDeleteAsync();
    }
}

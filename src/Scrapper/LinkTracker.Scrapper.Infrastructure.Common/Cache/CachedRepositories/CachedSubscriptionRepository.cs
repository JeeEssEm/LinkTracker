using Google.Protobuf.WellKnownTypes;
using LinkTracker.Scrapper.Domain.Tracking.Entities;
using LinkTracker.Scrapper.Domain.Tracking.Repositories;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace LinkTracker.Scrapper.Infrastructure.Pipelines.Cache.CachedRepositories;

public class CachedSubscriptionRepository(
    ISubscriptionRepository innerRepo,
    IConnectionMultiplexer cache,
    ILogger<CachedSubscriptionRepository> logger
) : ISubscriptionRepository
{
    public async Task<bool> CreateAsync(Subscription subscription)
    {
        await Invalidate(subscription.Id.ChatId.Value.ToString());
        return await innerRepo.CreateAsync(subscription);
    }

    public Task<Subscription?> GetAsync(ChatId chatId, string url)
    {
        return innerRepo.GetAsync(chatId, url);
    }

    public async Task RemoveAsync(Subscription subscription)
    {
        await Invalidate(subscription.Id.ChatId.Value.ToString());
        await innerRepo.RemoveAsync(subscription);
    }

    private async Task Invalidate(string key)
    {
        try
        {
            var db = cache.GetDatabase();
            await db.KeyDeleteAsync(key);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to invalidate cache");
        }
    }
}
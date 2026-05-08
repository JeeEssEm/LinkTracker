using LinkTracker.Scrapper.Domain.Tracking.Entities;
using LinkTracker.Scrapper.Domain.Tracking.Repositories;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace LinkTracker.Scrapper.Infrastructure.Pipelines.Cache.CachedRepositories;

public class CachedChatRepository(IChatRepository innerRepo, IConnectionMultiplexer cache, ILogger<CachedChatRepository> logger) : IChatRepository
{
    public Task<bool> AddChatAsync(ChatId telegramId) => innerRepo.AddChatAsync(telegramId);

    public Task<Chat?> GetChatAsync(ChatId id) => innerRepo.GetChatAsync(id);

    public async Task<bool> RemoveChatAsync(ChatId id)
    {
        await Invalidate(id.Value.ToString());
        return await innerRepo.RemoveChatAsync(id);
    }

    public Task<bool> ExistsAsync(ChatId id) => innerRepo.ExistsAsync(id);

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
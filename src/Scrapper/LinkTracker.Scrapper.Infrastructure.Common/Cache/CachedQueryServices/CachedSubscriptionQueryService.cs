using System.Text.Json;
using LinkTracker.Scrapper.Application.QueryServices;
using LinkTracker.Scrapper.Contracts.Dtos.Links;
using LinkTracker.Scrapper.Domain.Tracking.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace LinkTracker.Scrapper.Infrastructure.Pipelines.Cache.CachedQueryServices;

public class CachedSubscriptionQueryService(
    ISubscriptionQueryService innerService,
    IConnectionMultiplexer cache,
    ILogger<CachedSubscriptionQueryService> logger,
    IOptions<CacheConfiguration> config
) : ISubscriptionQueryService
{
    private readonly TimeSpan _cacheTtl = TimeSpan.FromSeconds(config.Value.CacheTimeToLiveSeconds);

    public async Task<IReadOnlyList<LinkResponse>> ListSubscriptionsByChatAsync(ChatId chatId)
    {
        var cacheKey = chatId.Value.ToString();

        try
        {
            var db = cache.GetDatabase();
            var cachedResult = await db.StringGetAsync(cacheKey);

            if (cachedResult.HasValue)
            {
                var result = JsonSerializer.Deserialize<IReadOnlyList<LinkResponse>>(cachedResult.ToString());
                if (result != null)
                {
                    return result;
                }
                logger.LogDebug("Cannot deserialize object from cache: {object}", result);
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to get value from cache");
        }

        var data = await innerService.ListSubscriptionsByChatAsync(chatId);
        await CacheDataAsync(cacheKey, data);

        return data;
    }

    private async Task CacheDataAsync<T>(string cacheKey, T data)
    {
        try
        {
            var db = cache.GetDatabase();
            var serialized = JsonSerializer.Serialize(data);
            await db.StringSetAsync(cacheKey, serialized, _cacheTtl);
            logger.LogDebug("Cached data for key: {CacheKey} with TTL: {Ttl}", cacheKey, _cacheTtl);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to cache data for key: {CacheKey}", cacheKey);
        }
    }
}
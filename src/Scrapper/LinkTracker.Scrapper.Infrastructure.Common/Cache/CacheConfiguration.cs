using StackExchange.Redis;

namespace LinkTracker.Scrapper.Infrastructure.Pipelines.Cache;

public class CacheConfiguration
{
    public const string Position = nameof(CacheConfiguration);
    public bool CacheEnabled { get; init; } = false;
    public bool ClientCacheEnabled { get; init; } = false;
    public int CacheTimeToLiveSeconds { get; init; } = 15 * 60; // 15 min
    public string? ConnectionString { get; init; }

    public void Validate()
    {
        if (CacheEnabled && string.IsNullOrWhiteSpace(ConnectionString))
        {
            throw new ArgumentException("ConnectionString for cache is not set!");
        }
    }
}

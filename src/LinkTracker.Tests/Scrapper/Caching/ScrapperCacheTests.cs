using System.Text.Json;
using LinkTracker.Scrapper.Contracts.Dtos.Links;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace LinkTracker.Tests.Scrapper.Caching;

public class ScrapperCacheTests(ScrapperWebApplicationFactory factory) : IClassFixture<ScrapperWebApplicationFactory>, IAsyncLifetime
{
    private HttpClient _client = null!;
    private IDatabase RedisClient => ConnectionMultiplexer.Connect(_redisConnectionString).GetDatabase();

    private readonly RedisContainer _redis = new RedisBuilder("valkey/valkey:8.0").Build();
    private string _redisConnectionString = string.Empty;

    public async Task InitializeAsync()
    {
        await _redis.StartAsync();
        _redisConnectionString = _redis.GetConnectionString();

        var testHost = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("CacheConfiguration:CacheEnabled", "true");
            builder.UseSetting("CacheConfiguration:ConnectionString", _redisConnectionString);
        });
        _client = testHost.CreateClient();
    }

    public async Task DisposeAsync()
    {
        await factory.DisposeAsync();
        await _redis.DisposeAsync();
    }

    [Fact]
    public async Task TestAddedLink()
    {
        var chatId = 123123L;
        var url = "https://github.com/some_user/his_cool_repo";
        await _client.PostAsync($"/tg-chat/{chatId}", null);
        await _client.PostLink(chatId, url, ["some_user", "new_repo"]);

        var res = await RedisClient.StringGetAsync(chatId.ToString());
        Assert.False(res.HasValue);

        await _client.GetLinks(chatId);
        var deserialized = await GetValueFromCache<IReadOnlyList<LinkResponse>>(chatId.ToString());
        Assert.NotNull(deserialized);
        Assert.Single(deserialized);
        Assert.True(deserialized[0].Url == url);
    }

    [Fact]
    public async Task TestRemoveLink()
    {
        var chatId = 456456L;
        var url = "https://github.com/poogle/public_cool_repo";
        await _client.PostAsync($"/tg-chat/{chatId}", null);
        await _client.PostLink(chatId, url, ["poogle"]);

        var res = await RedisClient.StringGetAsync(chatId.ToString());
        Assert.False(res.HasValue);

        await _client.GetLinks(chatId);
        res = await RedisClient.StringGetAsync(chatId.ToString());
        Assert.True(res.HasValue);

        await _client.DeleteLink(chatId, url);
        res = await RedisClient.StringGetAsync(chatId.ToString());
        Assert.False(res.HasValue);
    }

    [Fact]
    public async Task TestRemoveChat()
    {
        var chatId = 789L;
        var url = "https://github.com/dadot/corn_kernel";
        await _client.PostAsync($"/tg-chat/{chatId}", null);
        await _client.PostLink(chatId, url, ["dadot"]);

        var res = await RedisClient.StringGetAsync(chatId.ToString());
        Assert.False(res.HasValue);

        await _client.GetLinks(chatId);
        res = await RedisClient.StringGetAsync(chatId.ToString());
        Assert.True(res.HasValue);

        await _client.DeleteAsync($"/tg-chat/{chatId}");
        res = await RedisClient.StringGetAsync(chatId.ToString());
        Assert.False(res.HasValue);
    }

    [Fact]
    public async Task TestRefreshCacheAfterAddingLink()
    {
        var chatId = 789789L;
        var url1 = "https://github.com/dadot/corn_kernel";
        var url2 = "https://github.com/silicon_valley/apple_kernel";
        await _client.PostAsync($"/tg-chat/{chatId}", null);
        await _client.PostLink(chatId, url1, ["dadot"]);

        await _client.GetLinks(chatId);
        var deserialized = await GetValueFromCache<IReadOnlyList<LinkResponse>>(chatId.ToString());
        Assert.NotNull(deserialized);
        Assert.Single(deserialized);

        await _client.PostLink(chatId, url2, []);
        await _client.GetLinks(chatId);
        deserialized = await GetValueFromCache<IReadOnlyList<LinkResponse>>(chatId.ToString());
        Assert.NotNull(deserialized);
        Assert.Equal(2, deserialized.Count);
    }

    [Fact]
    public async Task TestDataRemovesAfterTtl()
    {
        var testHost = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("CacheConfiguration:CacheEnabled", "true");
            builder.UseSetting("CacheConfiguration:ConnectionString", _redisConnectionString);
            builder.UseSetting("CacheConfiguration:CacheTimeToLiveSeconds", "5");
        });
        _client = testHost.CreateClient();

        var chatId = 1011L;
        var url = "https://github.com/cars/tires";
        await _client.PostAsync($"/tg-chat/{chatId}", null);
        await _client.PostLink(chatId, url, []);
        await _client.GetLinks(chatId);

        var res = await GetValueFromCache<IReadOnlyList<LinkResponse>>(chatId.ToString());
        Assert.NotNull(res);

        await Task.Delay(5 * 1000);

        res = await GetValueFromCache<IReadOnlyList<LinkResponse>>(chatId.ToString());
        Assert.Null(res);
    }

    private async Task<T?> GetValueFromCache<T>(string key) where T : class
    {
        var res = await RedisClient.StringGetAsync(key);
        if (!res.HasValue)
        {
            return null;
        }

        return JsonSerializer.Deserialize<T>(res.ToString());
    }
}

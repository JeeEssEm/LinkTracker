using LinkTracker.Scrapper.Application.QueryServices;
using LinkTracker.Scrapper.Application.Repositories;
using LinkTracker.Scrapper.Domain.Tracking.Entities;
using LinkTracker.Scrapper.Domain.Tracking.Repositories;
using LinkTracker.Scrapper.Domain.Tracking.ValueObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace LinkTracker.Tests.Scrapper.Repositories;

public abstract class BaseRepositoriesTests(ScrapperWebApplicationFactory factory, string accessType)
    : IClassFixture<ScrapperWebApplicationFactory>, IAsyncLifetime
{
    private AsyncServiceScope _scope;
    private IServiceProvider _serviceProvider = null!;
    private string _connectionString = null!;

    private IChatRepository ChatRepo => _scope.ServiceProvider.GetRequiredService<IChatRepository>();
    private ILinkRepository LinkRepo => _scope.ServiceProvider.GetRequiredService<ILinkRepository>();
    private ISubscriptionRepository SubRepo => _scope.ServiceProvider.GetRequiredService<ISubscriptionRepository>();
    private ILinkBulkRepository LinkBulkRepo => _scope.ServiceProvider.GetRequiredService<ILinkBulkRepository>();

    private static long _idSeed = 10_000;
    private static long NextId() => Interlocked.Increment(ref _idSeed);

    private static string UniqueUrl(string label) => $"https://test-{label}.example/{NextId()}";

    public async Task InitializeAsync()
    {
        var testHost = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("DatabaseOptions:AccessType", accessType);
        });
        _ = testHost.CreateClient();

        _serviceProvider = testHost.Services;
        _scope = _serviceProvider.CreateAsyncScope();

        var config = _serviceProvider.GetRequiredService<IConfiguration>();
        _connectionString = config.GetValue<string>("DatabaseOptions:ConnectionString")!;

        await CleanupDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        await _scope.DisposeAsync();
    }

    [Fact]
    public async Task TestMigrationsApply()
    {
        var tables = await FetchListFromQuery<string>(
            "SELECT table_name FROM information_schema.tables " +
            "WHERE table_schema NOT IN ('pg_catalog', 'information_schema') AND table_type = 'BASE TABLE'"
        );

        Assert.Contains("links", tables);
        Assert.Contains("tags", tables);
        Assert.Contains("chats", tables);
        Assert.Contains("subscriptions", tables);
    }

    [Fact]
    public async Task AddNewChat()
    {
        var chatId = new ChatId(NextId());

        var chat = await ChatRepo.GetChatAsync(chatId);
        Assert.Null(chat);

        await ChatRepo.AddChatAsync(chatId);
        chat = await ChatRepo.GetChatAsync(chatId);
        Assert.NotNull(chat);
        Assert.Equal(chatId, chat.Id);

        var result = await FetchListFromQuery<long>(
            "SELECT id FROM chats WHERE id = @id",
            new NpgsqlParameter("id", chatId.Value));
        Assert.Single(result);
        Assert.Equal(chatId.Value, result[0]);
    }

    [Fact]
    public async Task RemoveChat()
    {
        var chatId = new ChatId(NextId());

        await ChatRepo.AddChatAsync(chatId);
        var chat = await ChatRepo.GetChatAsync(chatId);
        Assert.NotNull(chat);
        Assert.Equal(chatId, chat.Id);

        await ChatRepo.RemoveChatAsync(chatId);

        var result = await FetchListFromQuery<long>(
            "SELECT id FROM chats WHERE id = @id",
            new NpgsqlParameter("id", chatId.Value));
        Assert.Empty(result);
    }

    [Fact]
    public async Task CreateLink()
    {
        var url = UniqueUrl("create-link");

        var result = await FetchListFromQuery<int>(
                    "SELECT COUNT(*) FROM links WHERE url = @url",
                    new NpgsqlParameter("url", url));
        Assert.Equal(0, result[0]);

        var link = await LinkRepo.GetOrCreateAsync(new Uri(url), LinkSourceType.StackOverflow);
        Assert.NotNull(link);
        Assert.Equal(new Uri(url), link.LinkUri);

        var urls = await FetchListFromQuery<string>(
            "SELECT url FROM links WHERE url = @url",
            new NpgsqlParameter("url", url));
        Assert.Single(urls);
        Assert.Equal(url, urls[0]);
    }


    [Fact]
    public async Task RemoveLink()
    {
        var url = UniqueUrl("remove-link");

        await LinkRepo.GetOrCreateAsync(new Uri(url), LinkSourceType.StackOverflow);
        await LinkRepo.RemoveLinkAsync(new Uri(url));

        var result = await FetchListFromQuery<string>(
            "SELECT url FROM links WHERE url = @url",
            new NpgsqlParameter("url", url));
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetLinksByOwner()
    {
        var chatId = new ChatId(NextId());
        var url1 = UniqueUrl("owner-gh");
        var url2 = UniqueUrl("owner-so1");
        var url3 = UniqueUrl("owner-so2");

        await ChatRepo.AddChatAsync(chatId);
        var link1 = await LinkRepo.GetOrCreateAsync(new Uri(url1), LinkSourceType.Github);
        var link2 = await LinkRepo.GetOrCreateAsync(new Uri(url2), LinkSourceType.StackOverflow);
        await LinkRepo.GetOrCreateAsync(new Uri(url3), LinkSourceType.StackOverflow);

        await SubRepo.CreateAsync(new Subscription(new SubscriptionId(link1.Id, chatId), []));
        await SubRepo.CreateAsync(new Subscription(new SubscriptionId(link2.Id, chatId), []));

        var result = await FetchListFromQuery<long>(
            "SELECT COUNT(*) FROM subscriptions WHERE chat_id = @chatId",
            new NpgsqlParameter("chatId", chatId.Value));
        Assert.Equal(2, result[0]);
    }

    [Fact]
    public async Task GetLinksToCheckThenUpdate()
    {
        var chatId = new ChatId(NextId()); // Fix 2
        var url1 = UniqueUrl("check-gh");
        var url2 = UniqueUrl("check-so1");
        var url3 = UniqueUrl("check-so2");

        await ChatRepo.AddChatAsync(chatId);

        var link1 = await LinkRepo.GetOrCreateAsync(new Uri(url1), LinkSourceType.Github);
        var link2 = await LinkRepo.GetOrCreateAsync(new Uri(url2), LinkSourceType.StackOverflow);
        var link3 = await LinkRepo.GetOrCreateAsync(new Uri(url3), LinkSourceType.StackOverflow);

        await SubRepo.CreateAsync(new Subscription(new SubscriptionId(link1.Id, chatId), []));
        await SubRepo.CreateAsync(new Subscription(new SubscriptionId(link2.Id, chatId), []));
        await SubRepo.CreateAsync(new Subscription(new SubscriptionId(link3.Id, chatId), []));

        var linksToCheck = await LinkBulkRepo.GetLinksToCheck(1, 1000, DateTime.UtcNow);
        var initialCount = linksToCheck.Count;
        Assert.True(linksToCheck.Count >= 3);

        var beforeUpdate = DateTime.UtcNow;
        await LinkBulkRepo.UpdateCheckDatesAt([link1.Id.Value]);
        var afterUpdate = DateTime.UtcNow;

        linksToCheck = await LinkBulkRepo.GetLinksToCheck(1, 1000, beforeUpdate);
        Assert.Equal(initialCount - 1, linksToCheck.Count);

        var linkDate = await FetchListFromQuery<DateTime>(
            "SELECT last_check_at FROM links WHERE id = @id",
            new NpgsqlParameter("id", link1.Id.Value));
        Assert.Single(linkDate);
        Assert.InRange(linkDate[0], beforeUpdate, afterUpdate.AddSeconds(1));
    }


    [Fact]
    public async Task CreateLinkWithTags()
    {
        var chatId = new ChatId(NextId());
        var url = UniqueUrl("link-tags");

        await ChatRepo.AddChatAsync(chatId);
        var link = await LinkRepo.GetOrCreateAsync(new Uri(url), LinkSourceType.StackOverflow);

        await SubRepo.CreateAsync(new Subscription(new SubscriptionId(link.Id, chatId), [
            new Tag("пророк"),
            new Tag("брошюра"),
            new Tag("крекер"),
            new Tag("крен")
        ]));

        var subLink = await SubRepo.GetAsync(chatId, url);
        Assert.NotNull(subLink);
        var tags = subLink.Tags.Select(t => t.Name).ToArray();
        Assert.Contains("пророк", tags);
        Assert.Contains("брошюра", tags);
        Assert.Contains("крекер", tags);
        Assert.Contains("крен", tags);

        var connectedTags = await FetchListFromQuery<string>(
            """
            SELECT t.name FROM tags AS t
            INNER JOIN links AS l ON t.link_id = l.id AND t.chat_id = @chatId AND l.url = @url
            """,
            new NpgsqlParameter("chatId", chatId.Value),
            new NpgsqlParameter("url", url));
        Assert.Equal(4, connectedTags.Count);
        Assert.Contains("пророк", connectedTags);
        Assert.Contains("брошюра", connectedTags);
        Assert.Contains("крекер", connectedTags);
        Assert.Contains("крен", connectedTags);
    }

    [Fact]
    public async Task DeleteLinkWithTags()
    {
        var chatId = new ChatId(NextId());
        var url = UniqueUrl("delete-tags");

        await ChatRepo.AddChatAsync(chatId);
        var link = await LinkRepo.GetOrCreateAsync(new Uri(url), LinkSourceType.StackOverflow);
        await SubRepo.CreateAsync(new Subscription(new SubscriptionId(link.Id, chatId), [
            new Tag("indie"),
            new Tag("cold")
        ]));

        var subLink = await SubRepo.GetAsync(chatId, url);
        Assert.NotNull(subLink);
        Assert.Equal(2, subLink.Tags.Count);

        await SubRepo.RemoveAsync(subLink);
        subLink = await SubRepo.GetAsync(chatId, url);
        Assert.Null(subLink);

        var connectedTags = await FetchListFromQuery<string>(
            """
            SELECT t.name FROM tags AS t
            INNER JOIN links AS l ON t.link_id = l.id AND t.chat_id = @chatId AND l.url = @url
            """,
            new NpgsqlParameter("chatId", chatId.Value),
            new NpgsqlParameter("url", url));
        Assert.Empty(connectedTags);
    }

    [Fact]
    public async Task CheckPaginationOfLinksToCheck()
    {
        var chatId = new ChatId(NextId());

        await ChatRepo.AddChatAsync(chatId);

        var seed = NextId();
        var url1 = $"https://some-url-{seed}-1.com/";
        var url2 = $"https://some-url-{seed}-2.com/";
        var url3 = $"https://some-url-{seed}-3.com/";
        var url4 = $"https://some-url-{seed}-4.com/";
        var url5 = $"https://some-url-{seed}-5.com/";

        List<Link> links =
        [
            await LinkRepo.GetOrCreateAsync(new Uri(url1), LinkSourceType.Github),
            await LinkRepo.GetOrCreateAsync(new Uri(url2), LinkSourceType.Github),
            await LinkRepo.GetOrCreateAsync(new Uri(url3), LinkSourceType.Github),
            await LinkRepo.GetOrCreateAsync(new Uri(url4), LinkSourceType.Github),
            await LinkRepo.GetOrCreateAsync(new Uri(url5), LinkSourceType.Github),
        ];

        foreach (var link in links)
        {
            await SubRepo.CreateAsync(new Subscription(new SubscriptionId(link.Id, chatId), []));
        }

        var oldCheckAt = DateTime.SpecifyKind(new DateTime(2001, 1, 1, 0, 0, 0), DateTimeKind.Utc);
        await SetLinksLastCheckAt(links.Select(l => l.Id.Value).ToArray(), oldCheckAt);

        var cutoff = oldCheckAt.AddHours(1);
        var linksToCheck = await LinkBulkRepo.GetLinksToCheck(1, 1000, cutoff);
        Assert.Equal(5, linksToCheck.Count);
        Assert.Contains(url1, linksToCheck.Select(l => l.Url));

        await LinkBulkRepo.UpdateCheckDatesAt([links[0].Id.Value]);

        linksToCheck = await LinkBulkRepo.GetLinksToCheck(1, 1, cutoff);
        Assert.Single(linksToCheck);
        Assert.NotEqual(url1, linksToCheck[0].Url);

        linksToCheck = await LinkBulkRepo.GetLinksToCheck(1, 1000, cutoff);
        Assert.Equal(4, linksToCheck.Count);
        Assert.DoesNotContain(url1, linksToCheck.Select(l => l.Url));
    }

    private async Task CleanupDatabaseAsync()
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(
            "TRUNCATE TABLE subscriptions, tags, links, chats RESTART IDENTITY CASCADE;",
            connection);

        await command.ExecuteNonQueryAsync();
    }

    private async Task SetLinksLastCheckAt(Guid[] linkIds, DateTime timestamp)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(
            "UPDATE links SET last_check_at = @timestamp WHERE id = ANY(@linkIds);",
            connection);

        command.Parameters.AddWithValue("timestamp", timestamp);
        command.Parameters.AddWithValue("linkIds", linkIds);

        await command.ExecuteNonQueryAsync();
    }

    private async Task<List<T>> FetchListFromQuery<T>(string query, params NpgsqlParameter[] parameters)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(query, connection);
        command.Parameters.AddRange(parameters);

        await using var reader = await command.ExecuteReaderAsync();

        var result = new List<T>();
        while (await reader.ReadAsync())
        {
            result.Add(reader.GetFieldValue<T>(0));
        }

        return result;
    }
}
using LinkTracker.Scrapper.Application.Repositories;
using LinkTracker.Scrapper.Domain.Chats.Entities;
using LinkTracker.Scrapper.Domain.Links.Entities;
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
    private ITagRepository TagRepo => _scope.ServiceProvider.GetRequiredService<ITagRepository>();

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
        Assert.Contains("link_tags", tables);
    }

    [Fact]
    public async Task AddNewChat()
    {
        var chatId = NextId();

        var chat = await ChatRepo.GetChatAsync(chatId);
        Assert.Null(chat);

        await ChatRepo.AddChatAsync(new Chat(chatId));
        chat = await ChatRepo.GetChatAsync(chatId);
        Assert.NotNull(chat);
        Assert.Equal(chatId, chat.Id);

        var result = await FetchListFromQuery<long>(
            "SELECT id FROM chats WHERE id = @id",
            new NpgsqlParameter("id", chatId));
        Assert.Single(result);
        Assert.Equal(chatId, result[0]);
    }

    [Fact]
    public async Task RemoveChat()
    {
        var chatId = NextId();

        await ChatRepo.AddChatAsync(new Chat(chatId));
        var chat = await ChatRepo.GetChatAsync(chatId);
        Assert.NotNull(chat);
        Assert.Equal(chatId, chat.Id);

        await ChatRepo.RemoveChatAsync(chatId);

        var result = await FetchListFromQuery<long>(
            "SELECT id FROM chats WHERE id = @id",
            new NpgsqlParameter("id", chatId));
        Assert.Empty(result);
    }

    [Fact]
    public async Task CreateLink()
    {
        var url = UniqueUrl("create-link");

        Assert.Null(await LinkRepo.GetLinkAsync(url));

        await LinkRepo.CreateLink(url, "");
        var link = await LinkRepo.GetLinkAsync(url);
        Assert.NotNull(link);
        Assert.Equal(url, link.Url);

        var result = await FetchListFromQuery<string>(
            "SELECT url FROM links WHERE url = @url",
            new NpgsqlParameter("url", url));
        Assert.Single(result);
        Assert.Equal(url, result[0]);
    }

    [Fact]
    public async Task CreateExistingLink()
    {
        var url = UniqueUrl("create-existing");

        Assert.Null(await LinkRepo.GetLinkAsync(url));

        await LinkRepo.CreateLink(url, "");
        await Assert.ThrowsAnyAsync<Exception>(() => LinkRepo.CreateLink(url, ""));
    }

    [Fact]
    public async Task RemoveLink()
    {
        var url = UniqueUrl("remove-link");

        await LinkRepo.CreateLink(url, "");
        await LinkRepo.RemoveLink(url);

        var result = await FetchListFromQuery<string>(
            "SELECT url FROM links WHERE url = @url",
            new NpgsqlParameter("url", url));
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetLinksByOwner()
    {
        var chatId = NextId();
        var url1 = UniqueUrl("owner-gh");
        var url2 = UniqueUrl("owner-so1");
        var url3 = UniqueUrl("owner-so2");

        await ChatRepo.AddChatAsync(new Chat(chatId));
        var link1 = await LinkRepo.CreateLink(url1, "github");
        var link2 = await LinkRepo.CreateLink(url2, "stackoverflow");
        await LinkRepo.CreateLink(url3, "stackoverflow");

        await SubRepo.CreateSubscriptionAsync(chatId, link1.Id);
        await SubRepo.CreateSubscriptionAsync(chatId, link2.Id);

        var result = await FetchListFromQuery<long>(
            "SELECT COUNT(*) FROM subscriptions WHERE chat_id = @chatId",
            new NpgsqlParameter("chatId", chatId));
        Assert.Equal(2, result[0]);
    }

    [Fact]
    public async Task GetLinksToCheckThenUpdate()
    {
        var chatId = NextId(); // Fix 2
        var url1 = UniqueUrl("check-gh");
        var url2 = UniqueUrl("check-so1");
        var url3 = UniqueUrl("check-so2");

        await ChatRepo.AddChatAsync(new Chat(chatId));

        var link1 = await LinkRepo.CreateLink(url1, "github");
        var link2 = await LinkRepo.CreateLink(url2, "stackoverflow");
        var link3 = await LinkRepo.CreateLink(url3, "stackoverflow");

        await SubRepo.CreateSubscriptionAsync(chatId, link1.Id);
        await SubRepo.CreateSubscriptionAsync(chatId, link2.Id);
        await SubRepo.CreateSubscriptionAsync(chatId, link3.Id);

        var linksToCheck = await LinkRepo.GetLinksToCheck(1, 1000, DateTime.UtcNow);
        var initialCount = linksToCheck.Count;
        Assert.True(linksToCheck.Count >= 3);

        var beforeUpdate = DateTime.UtcNow;
        await LinkRepo.UpdateCheckDatesAt([link1.Id]);
        var afterUpdate = DateTime.UtcNow;

        linksToCheck = await LinkRepo.GetLinksToCheck(1, 1000, beforeUpdate);
        Assert.Equal(initialCount - 1, linksToCheck.Count);

        var linkDate = await FetchListFromQuery<DateTime>(
            "SELECT last_check_at FROM links WHERE id = @id",
            new NpgsqlParameter("id", link1.Id));
        Assert.Single(linkDate);
        Assert.InRange(linkDate[0], beforeUpdate, afterUpdate.AddSeconds(1));
    }

    [Fact]
    public async Task CreateAttachedToChatTags()
    {
        var chatId = NextId();

        await ChatRepo.AddChatAsync(new Chat(chatId));
        await TagRepo.CreateTags([".net", "F#", "gitara"], chatId);

        var tags = await FetchListFromQuery<string>(
            "SELECT name FROM tags WHERE chat_id = @chatId",
            new NpgsqlParameter("chatId", chatId));
        Assert.Equal(3, tags.Count);
        Assert.Contains("F#", tags);
        Assert.Contains(".net", tags);
        Assert.Contains("gitara", tags);
    }

    [Fact]
    public async Task CreateLinkWithTags()
    {
        var chatId = NextId();
        var url = UniqueUrl("link-tags");

        await ChatRepo.AddChatAsync(new Chat(chatId));
        var tags = await TagRepo.CreateTags(["пророк", "брошюра", "крекер", "крен"], chatId);
        var link = await LinkRepo.CreateLink(url, "hz");
        await SubRepo.CreateSubscriptionAsync(chatId, link.Id);
        await TagRepo.ConnectTagsToLink(link.Id, tags.Select(t => t.Id));

        var subLink = await SubRepo.GetSubscriptionAsync(chatId, link.Id);
        Assert.NotNull(subLink);
        Assert.Equal(url, subLink.Url);
        Assert.Contains("пророк", subLink.Tags);
        Assert.Contains("брошюра", subLink.Tags);
        Assert.Contains("крекер", subLink.Tags);
        Assert.Contains("крен", subLink.Tags);

        var connectedTags = await FetchListFromQuery<string>(
            """
            SELECT t.name FROM tags AS t
            INNER JOIN link_tags AS lt ON lt.tag_id = t.id
            INNER JOIN links AS l ON lt.link_id = l.id
            WHERE t.chat_id = @chatId AND l.url = @url
            """,
            new NpgsqlParameter("chatId", chatId),
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
        var chatId = NextId();
        var url = UniqueUrl("delete-tags");

        await ChatRepo.AddChatAsync(new Chat(chatId));
        var tags = await TagRepo.CreateTags(["indie", "cold"], chatId);
        var link = await LinkRepo.CreateLink(url, "unknown");
        await SubRepo.CreateSubscriptionAsync(chatId, link.Id);
        await TagRepo.ConnectTagsToLink(link.Id, tags.Select(t => t.Id));

        var subLink = await SubRepo.GetSubscriptionAsync(chatId, link.Id);
        Assert.NotNull(subLink);
        Assert.Equal(2, subLink.Tags.Count);

        await TagRepo.RemoveConnectionsTagsToLink(chatId, link.Id);
        subLink = await SubRepo.GetSubscriptionAsync(chatId, link.Id);
        Assert.NotNull(subLink);
        Assert.Empty(subLink.Tags);

        var tagIds = tags.Select(t => t.Id).ToArray();
        var inParams = tagIds.Select((_, i) => new NpgsqlParameter($"p{i}", tagIds[i])).ToArray();
        var inClause = string.Join(", ", inParams.Select(p => $"@{p.ParameterName}"));

        var orphanTags = await FetchListFromQuery<string>(
            $"SELECT name FROM tags WHERE id IN ({inClause})",
            inParams);
        Assert.Equal(2, orphanTags.Count);
        Assert.Contains("indie", orphanTags);
        Assert.Contains("cold", orphanTags);

        await TagRepo.RemoveTags(tags.Select(t => t.Id));

        inParams = tagIds.Select((_, i) => new NpgsqlParameter($"p{i}", tagIds[i])).ToArray();
        inClause = string.Join(", ", inParams.Select(p => $"@{p.ParameterName}"));
        orphanTags = await FetchListFromQuery<string>(
            $"SELECT name FROM tags WHERE id IN ({inClause})",
            inParams);
        Assert.Empty(orphanTags);
    }

    [Fact]
    public async Task CheckPaginationOfLinksToCheck()
    {
        var chatId = NextId();

        await ChatRepo.AddChatAsync(new Chat(chatId));

        var seed = NextId();
        var url1 = $"https://some-url-{seed}-1.com";
        var url2 = $"https://some-url-{seed}-2.com";
        var url3 = $"https://some-url-{seed}-3.com";
        var url4 = $"https://some-url-{seed}-4.com";
        var url5 = $"https://some-url-{seed}-5.com";

        List<Link> links =
        [
            await LinkRepo.CreateLink(url1, "unk"),
            await LinkRepo.CreateLink(url2, "unk"),
            await LinkRepo.CreateLink(url3, "unk"),
            await LinkRepo.CreateLink(url4, "unk"),
            await LinkRepo.CreateLink(url5, "unk"),
        ];

        foreach (var link in links)
        {
            await SubRepo.CreateSubscriptionAsync(chatId, link.Id);
        }

        var oldCheckAt = DateTime.SpecifyKind(new DateTime(2001, 1, 1, 0, 0, 0), DateTimeKind.Utc);
        await SetLinksLastCheckAt(links.Select(l => l.Id).ToArray(), oldCheckAt);

        var cutoff = oldCheckAt.AddHours(1);
        var linksToCheck = await LinkRepo.GetLinksToCheck(1, 1000, cutoff);
        Assert.Equal(5, linksToCheck.Count);
        Assert.Contains(url1, linksToCheck.Select(l => l.Url));

        await LinkRepo.UpdateCheckDatesAt([links[0].Id]);

        linksToCheck = await LinkRepo.GetLinksToCheck(1, 1, cutoff);
        Assert.Single(linksToCheck);
        Assert.NotEqual(url1, linksToCheck[0].Url);

        linksToCheck = await LinkRepo.GetLinksToCheck(1, 1000, cutoff);
        Assert.Equal(4, linksToCheck.Count);
        Assert.DoesNotContain(url1, linksToCheck.Select(l => l.Url));
    }

    private async Task CleanupDatabaseAsync()
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(
            "TRUNCATE TABLE link_tags, subscriptions, tags, links, chats RESTART IDENTITY CASCADE;",
            connection);

        await command.ExecuteNonQueryAsync();
    }

    private async Task SetLinksLastCheckAt(long[] linkIds, DateTime timestamp)
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
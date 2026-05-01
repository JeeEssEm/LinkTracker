using System.Text.Json;
using LinkTracker.Scrapper.Domain.Tracking.Entities;
using LinkTracker.Scrapper.Domain.Tracking.Repositories;
using FluentResults;
using LinkTracker.Bot.Contracts.Rest;
using LinkTracker.Scrapper.Application.Dtos.Outbox;
using LinkTracker.Scrapper.Application.QueryServices;
using LinkTracker.Scrapper.Application.Repositories;
using LinkTracker.Scrapper.Application.Scheduling;
using LinkTracker.Scrapper.Application.Scraping.ApiClients.Github;
using LinkTracker.Scrapper.Application.Scraping.ApiClients.Github.Models;
using LinkTracker.Scrapper.Application.Scraping.ApiClients.Github.Models.Statuses;
using LinkTracker.Scrapper.Application.Scraping.ApiClients.StackOverflow;
using LinkTracker.Scrapper.Application.Scraping.ApiClients.StackOverflow.Models;
using LinkTracker.Scrapper.Application.Scraping.Pipelines;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Store;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Notifications;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Store;
using LinkTracker.Scrapper.Domain.Tracking.SourceHandlers.Github;
using LinkTracker.Scrapper.Domain.Tracking.SourceHandlers.StackOverflow;
using LinkTracker.Scrapper.Domain.Tracking.ValueObjects;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Npgsql;

namespace LinkTracker.Tests.Scrapper.Pipelines.Integration;

public abstract class BasePipelineIntegrationTests(
    ScrapperWebApplicationFactory factory,
    string accessType)
    : IClassFixture<ScrapperWebApplicationFactory>, IAsyncLifetime
{
    private readonly IGithubClient _githubClient = Substitute.For<IGithubClient>();
    private readonly IStackOverflowClient _stackOverflowClient = Substitute.For<IStackOverflowClient>();
    private readonly InMemoryNotificationStrategy _notificationStrategy = new();

    private Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<LinkTracker.Scrapper.Program> _testHost;
    private AsyncServiceScope _scope;

    private static long _seed = 100_000;
    private static long NextId() => Interlocked.Increment(ref _seed);

    private IChatRepository ChatRepo => _scope.ServiceProvider.GetRequiredService<IChatRepository>();
    private ILinkRepository LinkRepo => _scope.ServiceProvider.GetRequiredService<ILinkRepository>();
    private ILinkBulkRepository LinkBulkRepo => _scope.ServiceProvider.GetRequiredService<ILinkBulkRepository>();
    private ISubscriptionRepository SubRepo => _scope.ServiceProvider.GetRequiredService<ISubscriptionRepository>();
    private ISubscriptionQueryService SubQueryRepo => _scope.ServiceProvider.GetRequiredService<ISubscriptionQueryService>();
    private IGithubStore GithubStore => _scope.ServiceProvider.GetRequiredService<IGithubStore>();
    private IGithubRepository GithubRepo => _scope.ServiceProvider.GetRequiredService<IGithubRepository>();
    private IStackOverflowStore StackOverflowStore => _scope.ServiceProvider.GetRequiredService<IStackOverflowStore>();
    private IStackOverflowRepository StackOverflowRepo => _scope.ServiceProvider.GetRequiredService<IStackOverflowRepository>();
    private ScrapingCoordinator Coordinator => _scope.ServiceProvider.GetRequiredService<ScrapingCoordinator>();

    public async Task InitializeAsync()
    {
        _testHost = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("DatabaseOptions:AccessType", accessType);
            builder.UseSetting("OutboxConfiguration:DelayBetweenPollsSeconds", "5");

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IGithubClient>();
                services.RemoveAll<IStackOverflowClient>();
                services.RemoveAll<INotificationStrategy>();

                services.AddScoped(_ => _githubClient);
                services.AddScoped(_ => _stackOverflowClient);

                services.AddSingleton(_notificationStrategy);
                services.AddScoped<INotificationStrategy>(sp => sp.GetRequiredService<InMemoryNotificationStrategy>());
            });
        });

        _ = _testHost.CreateClient();
        _scope = _testHost.Services.CreateAsyncScope();

        await ClearDatabaseAsync();

        SetupDefaultApiMocks();
    }

    public async Task DisposeAsync()
    {
        await _scope.DisposeAsync();
        if (_testHost != null)
        {
            await _testHost.DisposeAsync();
        }
    }

    [Fact]
    public async Task GithubNewIssue_SendsNotificationWithTitleAuthorAndPreview()
    {
        ResetMocksAndNotifications();

        var owner = $"owner-{NextId()}";
        var repo = $"repo-{NextId()}";
        var (chatId, link) = await CreateTrackedGithubLinkAsync(owner, repo);

        _githubClient.GetAllIssues(owner, repo, Arg.Any<int>())
            .Returns(Result.Ok<List<IssueResponse>>([
                new IssueResponse(
                    "Critical bug fixed",
                    42,
                    "alice",
                    DateTime.UtcNow,
                    "Issue preview text",
                    IssueStatus.Open)
            ]));

        await Coordinator.RunIterationAsync(batchSize: 50);

        var message = await WaitForNotificationByUrlAsync(link.LinkUri.ToString(), 10 * 1000);

        Assert.Equal(link.LinkUri.ToString(), message.Url);
        Assert.Contains(chatId, message.TgChatIds);
        Assert.Contains("Critical bug fixed", message.Description);
        Assert.Contains("alice", message.Description);
        Assert.Contains("Issue preview text", message.Description);
    }

    [Fact]
    public async Task StackOverflowNewAnswer_SendsNotification()
    {
        ResetMocksAndNotifications();

        var questionId = NextId();
        var (chatId, link) = await CreateTrackedStackOverflowLinkAsync(questionId);

        _stackOverflowClient.GetQuestions(Arg.Any<long[]>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(Result.Ok(new QuestionsResponse([
                new QuestionResponse(questionId, "How to test pipelines")
            ], false)));

        _stackOverflowClient.GetAnswers(Arg.Any<long[]>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(Result.Ok(new AnswersResponse([
                new AnswerResponse(questionId, questionId, "SO answer preview", "bob", DateTime.UtcNow)
            ], false)));

        await Coordinator.RunIterationAsync(batchSize: 50);

        var message = await WaitForNotificationByUrlAsync(link.LinkUri.ToString(), 10 * 1000);

        Assert.Equal(link.LinkUri.ToString(), message.Url);
        Assert.Contains(chatId, message.TgChatIds);
        Assert.Contains("Новый ответ от bob", message.Description);
        Assert.Contains("SO answer preview", message.Description);
    }

    [Fact]
    public async Task ExternalApiUnavailable_IsHandledWithoutCrashing()
    {
        ResetMocksAndNotifications();

        var owner = $"owner-{NextId()}";
        var repo = $"repo-{NextId()}";
        await CreateTrackedGithubLinkAsync(owner, repo);

        _githubClient.GetAllIssues(owner, repo, Arg.Any<int>())
            .Returns(Result.Fail<List<IssueResponse>>("api unavailable"));

        await Coordinator.RunIterationAsync(batchSize: 50);

        var notifications = await WaitForNotificationsAsync(0, timeoutMs: 500);
        Assert.Empty(notifications);
    }

    [Fact]
    public async Task LongPreview_IsTruncatedTo200Characters()
    {
        ResetMocksAndNotifications();

        var owner = $"owner-{NextId()}";
        var repo = $"repo-{NextId()}";
        await CreateTrackedGithubLinkAsync(owner, repo);

        var longDescription = new string('x', 260);

        _githubClient.GetAllIssues(owner, repo, Arg.Any<int>())
            .Returns(Result.Ok<List<IssueResponse>>([
                new IssueResponse(
                    "Very long issue",
                    43,
                    "alice",
                    DateTime.UtcNow,
                    longDescription,
                    IssueStatus.Open)
            ]));

        await Coordinator.RunIterationAsync(batchSize: 50);

        var message = await WaitForNotificationByUrlAsync($"https://github.com/{owner}/{repo}", 10 * 1000);

        Assert.Contains(new string('x', 180), message.Description);
        Assert.DoesNotContain(new string('x', 230), message.Description);
    }

    [Fact]
    public async Task BatchProcessing_PartialFailure_DoesNotBreakOtherLinks()
    {
        ResetMocksAndNotifications();

        var owner1 = $"owner-{NextId()}";
        var repo1 = $"repo-{NextId()}";
        var owner2 = $"owner-{NextId()}";
        var repo2 = $"repo-{NextId()}";

        var (_, failedLink) = await CreateTrackedGithubLinkAsync(owner1, repo1);
        var (chatId2, successLink) = await CreateTrackedGithubLinkAsync(owner2, repo2);

        _githubClient.GetAllIssues(owner1, repo1, Arg.Any<int>())
            .Returns(Result.Fail<List<IssueResponse>>("temporary failure"));

        _githubClient.GetAllIssues(owner2, repo2, Arg.Any<int>())
            .Returns(Result.Ok<List<IssueResponse>>([
                new IssueResponse("Works", 5, "eve", DateTime.UtcNow, "ok", IssueStatus.Open)
            ]));

        await Coordinator.RunIterationAsync(batchSize: 50);

        var notifications = await WaitForNotificationsAsync(1, 10 * 1000);
        var message = Assert.Single(notifications.Where(n => n.Url == successLink.LinkUri.AbsoluteUri));

        Assert.Equal(successLink.LinkUri.AbsoluteUri, message.Url);
        Assert.Contains(chatId2, message.TgChatIds);
        Assert.NotEqual(failedLink.LinkUri.AbsoluteUri, message.Url);
        Assert.DoesNotContain(notifications, n => n.Url == failedLink.LinkUri.AbsoluteUri && n.Description.Contains("Новое Issue"));
    }

    [Fact]
    public async Task SubscriptionCreatesAndPersistsRequiredEntities()
    {
        ResetMocksAndNotifications();

        var owner = $"owner-{NextId()}";
        var repo = $"repo-{NextId()}";
        var questionId = NextId();

        var (chatGithub, githubLink) = await CreateTrackedGithubLinkAsync(owner, repo);
        var (chatSo, soLink) = await CreateTrackedStackOverflowLinkAsync(questionId);

        Assert.NotNull(await SubRepo.GetAsync(new ChatId(chatGithub), githubLink.LinkUri.AbsoluteUri));
        Assert.NotNull(await SubRepo.GetAsync(new ChatId(chatSo), soLink.LinkUri.AbsoluteUri));

        var githubMappings = await GithubStore.GetRepositoriesByLinkIdsAsync([githubLink.Id.Value]);
        Assert.True(githubMappings.ContainsKey(githubLink.Id.Value));

        var soMappings = await StackOverflowStore.GetQuestionIdsByLinkIdsAsync([soLink.Id.Value]);
        Assert.True(soMappings.ContainsKey(soLink.Id.Value));
        Assert.Equal(questionId, soMappings[soLink.Id.Value]);
    }

    private async Task<(long ChatId, Link Link)> CreateTrackedGithubLinkAsync(string owner, string repo)
    {
        var chatId = NextId();
        var url = $"https://github.com/{owner}/{repo}";

        await ChatRepo.AddChatAsync(new ChatId(chatId));
        var link = await LinkRepo.GetOrCreateAsync(new Uri(url), LinkSourceType.Github);
        await SubRepo.CreateAsync(new Subscription(new SubscriptionId(link.Id, new ChatId(chatId)), []));

        await GithubRepo.CreateAsync(link.Id, repo, owner);
        await LinkBulkRepo.UpdateCheckDatesAt([link.Id.Value]);

        return (chatId, link);
    }

    private async Task<(long ChatId, Link Link)> CreateTrackedStackOverflowLinkAsync(long questionId)
    {
        var chatId = NextId();
        var url = $"https://stackoverflow.com/questions/{questionId}";

        await ChatRepo.AddChatAsync(new ChatId(chatId));
        var link = await LinkRepo.GetOrCreateAsync(new Uri(url), LinkSourceType.StackOverflow);
        await SubRepo.CreateAsync(new Subscription(new SubscriptionId(link.Id, new ChatId(chatId)), []));

        await StackOverflowRepo.CreateQuestionAsync(link.Id, questionId);
        await LinkBulkRepo.UpdateCheckDatesAt([link.Id.Value]);

        return (chatId, link);
    }

    private void ResetMocksAndNotifications()
    {
        _notificationStrategy.Clear();
        SetupDefaultApiMocks();
    }

    private void SetupDefaultApiMocks()
    {
        _githubClient.GetAllIssues(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>())
            .Returns(Result.Ok<List<IssueResponse>>([]));

        _githubClient.GetAllPullRequests(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>())
            .Returns(Result.Ok<List<PullRequestResponse>>([]));

        _stackOverflowClient.GetQuestions(Arg.Any<long[]>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(Result.Ok(new QuestionsResponse([], false)));

        _stackOverflowClient.GetAnswers(Arg.Any<long[]>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(Result.Ok(new AnswersResponse([], false)));

        _stackOverflowClient.GetComments(Arg.Any<long[]>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(Result.Ok(new CommentsResponse([], false)));

        _stackOverflowClient.GetQuestionComments(Arg.Any<long[]>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(Result.Ok(new CommentsResponse([], false)));
    }

    private async Task ClearDatabaseAsync()
    {
        var config = _scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var connectionString = config.GetValue<string>("DatabaseOptions:ConnectionString")
                               ?? throw new InvalidOperationException("DatabaseOptions:ConnectionString is not configured.");

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        const string truncateSql = """
            TRUNCATE TABLE
                subscriptions,
                tags,
                chats,
                stackoverflow_question_comments,
                stackoverflow_comments,
                stackoverflow_answers,
                stackoverflow_questions,
                github_pull_requests,
                github_issues,
                github_repositories,
                links
            RESTART IDENTITY CASCADE;
            """;

        await using var command = new NpgsqlCommand(truncateSql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<IReadOnlyList<LinkUpdateRequest>> WaitForNotificationsAsync(int expectedCount, int timeoutMs = 5000)
    {
        var startedAt = DateTime.UtcNow;
        while ((DateTime.UtcNow - startedAt).TotalMilliseconds < timeoutMs)
        {
            var snapshot = _notificationStrategy.Snapshot();
            if (snapshot.Count >= expectedCount)
            {
                return snapshot;
            }

            await Task.Delay(50);
        }

        return _notificationStrategy.Snapshot();
    }

    private async Task<LinkUpdateRequest> WaitForNotificationByUrlAsync(string url, int timeoutMs = 5000)
    {
        var startedAt = DateTime.UtcNow;
        while ((DateTime.UtcNow - startedAt).TotalMilliseconds < timeoutMs)
        {
            var snapshot = _notificationStrategy.Snapshot();
            var match = snapshot.FirstOrDefault(n => n.Url == url);
            if (match is not null)
            {
                return match;
            }

            await Task.Delay(50);
        }

        throw new Xunit.Sdk.XunitException($"Notification for URL '{url}' was not received within {timeoutMs} ms.");
    }

    private sealed class InMemoryNotificationStrategy : INotificationStrategy
    {
        private readonly List<LinkUpdateRequest> _messages = [];
        private readonly Lock _lock = new();

        public Task<SendResult> SendAsync(OutboxEvent[] messages, CancellationToken ct)
        {
            var parsed = messages.Select(e => JsonSerializer.Deserialize<LinkUpdateRequest>(e.Payload))
                .Where(e => e != null)
                .ToArray();

            lock (_lock)
            {
                _messages.AddRange(parsed);
            }

            return Task.FromResult(new SendResult(messages.Select(e => e.Id).ToList(), Array.Empty<FailedToDeliver>()));
        }

        public IReadOnlyList<LinkUpdateRequest> Snapshot()
        {
            lock (_lock)
            {
                return _messages.ToList();
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _messages.Clear();
            }
        }
    }
}

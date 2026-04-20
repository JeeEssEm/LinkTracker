using FluentResults;
using LinkTracker.Scrapper.Application.Repositories;
using LinkTracker.Scrapper.Application.Scheduling;
using LinkTracker.Scrapper.Application.Scraping.ApiClients.Github;
using LinkTracker.Scrapper.Application.Scraping.ApiClients.Github.Models;
using LinkTracker.Scrapper.Application.Scraping.ApiClients.Github.Models.Statuses;
using LinkTracker.Scrapper.Application.Scraping.ApiClients.StackOverflow;
using LinkTracker.Scrapper.Application.Scraping.ApiClients.StackOverflow.Models;
using LinkTracker.Scrapper.Application.Scraping.LinkCheckers;
using LinkTracker.Scrapper.Application.Scraping.Pipelines;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Store;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Notifications;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Store;
using LinkTracker.Scrapper.Domain.Chats.Entities;
using LinkTracker.Scrapper.Domain.Links.Entities;
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

    private AsyncServiceScope _scope;

    private static long _seed = 100_000;
    private static long NextId() => Interlocked.Increment(ref _seed);

    private IChatRepository ChatRepo => _scope.ServiceProvider.GetRequiredService<IChatRepository>();
    private ILinkRepository LinkRepo => _scope.ServiceProvider.GetRequiredService<ILinkRepository>();
    private ISubscriptionRepository SubRepo => _scope.ServiceProvider.GetRequiredService<ISubscriptionRepository>();
    private IGithubStore GithubStore => _scope.ServiceProvider.GetRequiredService<IGithubStore>();
    private IStackOverflowStore StackOverflowStore => _scope.ServiceProvider.GetRequiredService<IStackOverflowStore>();
    private ScrapingCoordinator Coordinator => _scope.ServiceProvider.GetRequiredService<ScrapingCoordinator>();

    public async Task InitializeAsync()
    {
        var testHost = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("DatabaseOptions:AccessType", accessType);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IGithubClient>();
                services.RemoveAll<IStackOverflowClient>();
                services.RemoveAll<INotificationStrategy>();
                services.RemoveAll<NotificationAggregator>();

                services.AddScoped(_ => _githubClient);
                services.AddScoped(_ => _stackOverflowClient);

                services.AddSingleton(_notificationStrategy);
                services.AddScoped<INotificationStrategy>(sp => sp.GetRequiredService<InMemoryNotificationStrategy>());
                services.AddSingleton(sp =>
                    new NotificationAggregator(NullLogger<NotificationAggregator>.Instance, sp, batchSize: 1));
            });
        });

        _ = testHost.CreateClient();
        _scope = testHost.Services.CreateAsyncScope();

        await ClearDatabaseAsync();

        SetupDefaultApiMocks();
    }

    public async Task DisposeAsync()
    {
        await _scope.DisposeAsync();
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

        var message = await WaitForNotificationByUrlAsync(link.Url);

        Assert.Equal(link.Url, message.Url);
        Assert.Contains(chatId, message.UserIdsToNotify);
        Assert.Contains("Critical bug fixed", message.Message);
        Assert.Contains("alice", message.Message);
        Assert.Contains("Issue preview text", message.Message);
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

        var message = await WaitForNotificationByUrlAsync(link.Url, 10_000);

        Assert.Equal(link.Url, message.Url);
        Assert.Contains(chatId, message.UserIdsToNotify);
        Assert.Contains("Новый ответ от bob", message.Message);
        Assert.Contains("SO answer preview", message.Message);
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

        var message = await WaitForNotificationByUrlAsync(url: $"https://github.com/{owner}/{repo}");

        Assert.Contains(new string('x', 180), message.Message);
        Assert.DoesNotContain(new string('x', 230), message.Message);
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

        var notifications = await WaitForNotificationsAsync(1);
        var message = Assert.Single(notifications.Where(n => n.Url == successLink.Url));

        Assert.Equal(successLink.Url, message.Url);
        Assert.Contains(chatId2, message.UserIdsToNotify);
        Assert.NotEqual(failedLink.Url, message.Url);
        Assert.DoesNotContain(notifications, n => n.Url == failedLink.Url && n.Message.Contains("Новое Issue"));
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

        Assert.True(await SubRepo.SubscriptionExistsAsync(chatGithub, githubLink.Id));
        Assert.True(await SubRepo.SubscriptionExistsAsync(chatSo, soLink.Id));

        var githubMappings = await GithubStore.GetRepositoriesByLinkIdsAsync([githubLink.Id]);
        Assert.True(githubMappings.ContainsKey(githubLink.Id));

        var soMappings = await StackOverflowStore.GetQuestionIdsByLinkIdsAsync([soLink.Id]);
        Assert.True(soMappings.ContainsKey(soLink.Id));
        Assert.Equal(questionId, soMappings[soLink.Id]);
    }

    private async Task<(long ChatId, Link Link)> CreateTrackedGithubLinkAsync(string owner, string repo)
    {
        var chatId = NextId();
        var url = $"https://github.com/{owner}/{repo}";

        await ChatRepo.AddChatAsync(new Chat(chatId));
        var link = await LinkRepo.CreateLink(url, GithubRepositoryChecker.ConstKey);
        await SubRepo.CreateSubscriptionAsync(chatId, link.Id);

        await GithubStore.CreateRepository(link.Id, repo, owner);
        await LinkRepo.UpdateCheckDatesAt([link.Id]);

        return (chatId, link);
    }

    private async Task<(long ChatId, Link Link)> CreateTrackedStackOverflowLinkAsync(long questionId)
    {
        var chatId = NextId();
        var url = $"https://stackoverflow.com/questions/{questionId}";

        await ChatRepo.AddChatAsync(new Chat(chatId));
        var link = await LinkRepo.CreateLink(url, StackOverflowQuestionChecker.ConstKey);
        await SubRepo.CreateSubscriptionAsync(chatId, link.Id);

        await StackOverflowStore.CreateQuestion(link.Id, questionId);
        await LinkRepo.UpdateCheckDatesAt([link.Id]);

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
                link_tags,
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

    private async Task<IReadOnlyList<NotificationMessage>> WaitForNotificationsAsync(int expectedCount, int timeoutMs = 5000)
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

    private async Task<NotificationMessage> WaitForNotificationByUrlAsync(string url, int timeoutMs = 5000)
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
        private readonly List<NotificationMessage> _messages = [];
        private readonly Lock _lock = new();

        public string TransportName => "test";

        public Task SendAsync(NotificationMessage[] messages, CancellationToken ct)
        {
            lock (_lock)
            {
                _messages.AddRange(messages);
            }

            return Task.CompletedTask;
        }

        public IReadOnlyList<NotificationMessage> Snapshot()
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

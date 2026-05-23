using System.Text.Json;
using Confluent.Kafka;
using FluentResults;
using LinkTracker.Bot.Contracts.Rest;
using LinkTracker.Scrapper.Application.Repositories;
using LinkTracker.Scrapper.Application.Scraping.ApiClients.Github;
using LinkTracker.Scrapper.Application.Scraping.ApiClients.Github.Models;
using LinkTracker.Scrapper.Application.Scraping.ApiClients.Github.Models.Statuses;
using LinkTracker.Scrapper.Application.Scraping.ApiClients.StackOverflow;
using LinkTracker.Scrapper.Application.Scraping.ApiClients.StackOverflow.Models;
using LinkTracker.Scrapper.Application.Scraping.Pipelines;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Store;
using LinkTracker.Scrapper.Domain.Tracking.Entities;
using LinkTracker.Scrapper.Domain.Tracking.Repositories;
using LinkTracker.Scrapper.Domain.Tracking.Services;
using LinkTracker.Scrapper.Domain.Tracking.ValueObjects;
using LinkTracker.Tests.Bot;
using LinkTracker.Tests.Scrapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Telegram.Bot;
using Telegram.Bot.Requests;
using Testcontainers.Kafka;

namespace LinkTracker.Tests.Interservice.Broker;

public class KafkaMessagingTests : IAsyncLifetime
{
    private string _bootstrapServers;
    private string _topic;

    private readonly KafkaContainer _kafka = new KafkaBuilder("apache/kafka:3.8.1").Build();
    private ScrapperWebApplicationFactory _scrapperFactory = null!;
    private BotWebApplicationFactory _botFactory = null!;
    private HttpClient _scrapperClient = null!;
    private IServiceProvider _scrapperServices = null!;
    private IGithubClient _githubClient = Substitute.For<IGithubClient>();
    private IStackOverflowClient _stackOverflowClient = Substitute.For<IStackOverflowClient>();

    public async Task InitializeAsync()
    {
        await _kafka.StartAsync();

        void SetTopicSettings(IWebHostBuilder builder)
        {
            builder.UseSetting("TopicConfiguration:NumPartitions", "1");
            builder.UseSetting("TopicConfiguration:ReplicationFactor", "1");
            builder.UseSetting("TopicConfiguration:InSyncReplicas", "1");
        }

        _bootstrapServers = _kafka.GetBootstrapAddress();
        _topic = "updates";

        _scrapperFactory = new ScrapperWebApplicationFactory();
        await _scrapperFactory.InitializeAsync();

        var scrapperConfigured = _scrapperFactory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("NotificationStrategyOptions:BootstrapServers", _bootstrapServers);
            builder.UseSetting("NotificationStrategyOptions:Topic", _topic);
            builder.UseSetting("NotificationStrategyOptions:Strategy", "0");
            builder.UseSetting("BotClientOptions:UseGrpc", "true");
            builder.UseSetting("OutboxConfiguration:DelayBetweenPollsSeconds", "1");
            builder.UseSetting("DatabaseOptions:AccessType", "SQL");
            SetTopicSettings(builder);

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IGithubClient>();
                services.RemoveAll<IStackOverflowClient>();
                services.AddScoped(_ => _githubClient);
                services.AddScoped(_ => _stackOverflowClient);
            });
        });

        _botFactory = new BotWebApplicationFactory();
        var botConfigured = _botFactory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("KafkaConsumerOptions:KafkaConsumerEnabled", "true");
            builder.UseSetting("KafkaConsumerOptions:Topic", _topic);
            builder.UseSetting("KafkaConsumerOptions:BootstrapServers", _bootstrapServers);
            builder.UseSetting("KafkaConsumerOptions:GroupId", "notifiers");
            SetTopicSettings(builder);

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ConsumerConfig>();
                services.AddSingleton(new ConsumerConfig()
                {
                    BootstrapServers = _bootstrapServers,
                    GroupId = "notifiers",
                    AutoOffsetReset = AutoOffsetReset.Earliest,
                    BrokerAddressFamily = BrokerAddressFamily.V4
                });
            });

            builder.ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
            });
        });

        _scrapperClient = scrapperConfigured.CreateClient();
        _scrapperServices = scrapperConfigured.Services;
        _ = botConfigured.CreateClient();

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

    public async Task DisposeAsync()
    {
        await _kafka.DisposeAsync();
        await _scrapperFactory.DisposeAsync();
        await _botFactory.DisposeAsync();
    }

    [Fact]
    public async Task Test_Bot_Receives_SimpleMessage()
    {
        using var producer = new ProducerBuilder<Null, string>(new ProducerConfig()
        {
            BootstrapServers = _bootstrapServers,
            BrokerAddressFamily = BrokerAddressFamily.V4
        }).Build();
        var chats = new List<long> { 1, 2, 3 };

        var report = await producer.ProduceAsync(_topic, new Message<Null, string>()
        {
            Value = JsonSerializer.Serialize(
                new LinkUpdateRequest(Guid.NewGuid(), "https://hello.com", "new notification sdnly", chats, "HIGH")
                )
        }
        );

        if (report.Status == PersistenceStatus.NotPersisted)
        {
            throw new Exception($"Delivery failed: {report.Value}");
        }
        producer.Flush(TimeSpan.FromSeconds(0));
        await Task.Delay(5000);

        await _botFactory.BotMock.Received(3).SendRequest(
            Arg.Is<SendMessageRequest>(req =>
                chats.Contains(req.ChatId.Identifier ?? -1) &&
                req.Text.Contains("sdnly")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Test_ScrapperIterates_SendsUpdate_To_Kafka_Then_ToBot()
    {
        var owner = "some_owner";
        var repo = "some_repo";

        _githubClient.GetAllIssues(owner, repo, Arg.Any<int>())
            .Returns(Result.Ok<List<IssueResponse>>([
                new IssueResponse("New critical issue", 1, "test_user", DateTime.UtcNow, "issue comment", IssueStatus.Open)
            ]));

        await using var scope = _scrapperServices.CreateAsyncScope();
        var chatRepo = scope.ServiceProvider.GetRequiredService<IChatRepository>();
        var linkManager = scope.ServiceProvider.GetRequiredService<LinkManager>();
        var subRepo = scope.ServiceProvider.GetRequiredService<ISubscriptionRepository>();
        var linkBulkRepo = scope.ServiceProvider.GetRequiredService<ILinkBulkRepository>();

        var chatId = new ChatId(444L);
        var url = $"https://github.com/{owner}/{repo}";

        await chatRepo.AddChatAsync(chatId);

        var link = await linkManager.GetOrCreateAsync(new Uri(url));
        await subRepo.CreateAsync(new Subscription(new SubscriptionId(link.Value.Id, chatId), []));

        await linkBulkRepo.UpdateCheckDatesAt([link.Value.Id.Value]);

        var coordinator = scope.ServiceProvider.GetRequiredService<ScrapingCoordinator>();
        await coordinator.RunIterationAsync(100);

        await Task.Delay(5000);

        await _botFactory.BotMock.Received(1).SendRequest(
            Arg.Is<SendMessageRequest>(req =>
                req.ChatId.Identifier == chatId.Value &&
                req.Text != null && req.Text.Contains("New critical issue")),
            Arg.Any<CancellationToken>());
    }
}
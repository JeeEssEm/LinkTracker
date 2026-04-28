using Confluent.Kafka;
using Dapper;
using FluentResults;
using LinkTracker.Bot.Contracts.Rest;
using LinkTracker.Scrapper.Application.Dtos.Outbox;
using LinkTracker.Scrapper.Application.Repositories;
using LinkTracker.Scrapper.Application.Scraping.ApiClients.Bot;
using LinkTracker.Scrapper.Infrastructure.Orm;
using LinkTracker.Scrapper.Infrastructure.Orm.Context;
using LinkTracker.Tests.Scrapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Xunit;
using Microsoft.AspNetCore.Hosting;
using LinkTracker.Scrapper.Infrastructure.Pipelines.Outbox;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Testcontainers.Kafka;

namespace LinkTracker.Tests.Interservice.Outbox;

public class OutboxIntegrationTests : IAsyncLifetime
{
    private ScrapperWebApplicationFactory _factory = null!;
    private readonly KafkaContainer _kafka = new KafkaBuilder("apache/kafka:3.8.1").Build();

    private string _connectionString = null!;

    public async Task InitializeAsync()
    {
        _factory = new ScrapperWebApplicationFactory();
        await _factory.InitializeAsync();
        await _kafka.StartAsync();

        var config = _factory.Services.GetRequiredService<IConfiguration>();
        _connectionString = config.GetValue<string>("DatabaseOptions:ConnectionString")!;
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _kafka.DisposeAsync();
    }

    private async Task CleanupEventsAsync()
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(
            "TRUNCATE TABLE outbox_events RESTART IDENTITY CASCADE;",
            connection);

        await command.ExecuteNonQueryAsync();
    }
    private async Task<List<(long id, int status)>> GetEventsAsync()
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        var result = await connection.QueryAsync<(long id, int status)>(
            "SELECT id, status FROM outbox_events"
        );

        return result.ToList();
    }

    [Fact]
    public async Task Test_KafkaStrategy_SendsMessages_And_UpdatesStatus_To_Sent()
    {
        var kafkaProducerMock = Substitute.For<IProducer<Null, string>>();
        var configuredFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("OutboxConfiguration:DelayBetweenPollsSeconds", "1");
            builder.UseSetting("NotificationStrategyOptions:Strategy", "0"); // Kafka
            builder.UseSetting("TopicConfiguration:NumPartitions", "1");
            builder.UseSetting("TopicConfiguration:ReplicationFactor", "1");
            builder.UseSetting("TopicConfiguration:InSyncReplicas", "1");
            builder.UseSetting("NotificationStrategyOptions:BootstrapServers", _kafka.GetBootstrapAddress());

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IProducer<Null, string>>();
                services.AddScoped(_ => kafkaProducerMock);
            });
        });

        await using var scope = configuredFactory.Services.CreateAsyncScope();
        var outboxRepo = scope.ServiceProvider.GetRequiredService<IOutboxEventRepository>();

        await CleanupEventsAsync();

        var events = new[]
        {
            new CreateOutboxEvent(new LinkUpdateRequest(1, "https://example.com", "Test Message Kafka", new[] { 100L }))
        };

        await outboxRepo.CreateAsync(events);

        kafkaProducerMock.ProduceAsync(Arg.Any<string>(), Arg.Any<Message<Null, string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new DeliveryResult<Null, string> { Status = PersistenceStatus.Persisted }));

        var bgService = scope.ServiceProvider.GetServices<Microsoft.Extensions.Hosting.IHostedService>()
            .OfType<OutboxBackgroundService>()
            .FirstOrDefault();

        Assert.NotNull(bgService);

        var cts = new CancellationTokenSource();
        var bgTask = bgService.StartAsync(cts.Token);

        await Task.Delay(4000);
        await cts.CancelAsync();

        var dbEvents = await GetEventsAsync();
        Assert.Single(dbEvents);
        Assert.Equal((int)EventStatus.Sent, dbEvents[0].status);
    }

    [Fact]
    public async Task Test_HttpGrpcStrategy_SendsMessages_And_UpdatesStatus_To_Sent()
    {
        var botClientMock = Substitute.For<IBotClient>();
        var configuredFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("OutboxConfiguration:DelayBetweenPollsSeconds", "1");
            builder.UseSetting("NotificationStrategyOptions:Strategy", "1");
            builder.UseSetting("BotClientOptions:UseGrpc", "true");

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IBotClient>();
                services.AddScoped(_ => botClientMock);
            });
        });

        await using var scope = configuredFactory.Services.CreateAsyncScope();
        var outboxRepo = scope.ServiceProvider.GetRequiredService<IOutboxEventRepository>();

        await CleanupEventsAsync();

        var events = new[]
        {
            new CreateOutboxEvent(new LinkUpdateRequest(2, "https://example2.com", "Test Message gRPC", new[] { 200L }))
        };

        await outboxRepo.CreateAsync(events);

        botClientMock.SendUpdateAsync(Arg.Any<LinkUpdateRequest>())
            .Returns(Task.FromResult(Result.Ok()));

        var bgService = scope.ServiceProvider.GetServices<Microsoft.Extensions.Hosting.IHostedService>()
            .OfType<OutboxBackgroundService>()
            .FirstOrDefault();

        Assert.NotNull(bgService);

        var cts = new CancellationTokenSource();
        var bgTask = bgService.StartAsync(cts.Token);

        await Task.Delay(4000);
        await cts.CancelAsync();

        var dbEvents = await GetEventsAsync();
        Assert.Single(dbEvents);
        Assert.Equal((int)EventStatus.Sent, dbEvents[0].status);
    }

    [Fact]
    public async Task Test_Strategy_NetworkFail_UpdatesStatusTo_Failed_With_RetryLog()
    {
        var botClientMock = Substitute.For<IBotClient>();
        var configuredFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("OutboxConfiguration:DelayBetweenPollsSeconds", "1");
            builder.UseSetting("NotificationStrategyOptions:Strategy", "1");
            builder.UseSetting("BotClientOptions:UseGrpc", "true");

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IBotClient>();
                services.AddScoped(_ => botClientMock);
            });
        });

        await using var scope = configuredFactory.Services.CreateAsyncScope();
        var outboxRepo = scope.ServiceProvider.GetRequiredService<IOutboxEventRepository>();

        await CleanupEventsAsync();

        var events = new[]
        {
            new CreateOutboxEvent(new LinkUpdateRequest(3, "https://example3.com", "Test Network Fail", new[] { 300L }))
        };

        await outboxRepo.CreateAsync(events);

        // Mock network exception
        botClientMock.SendUpdateAsync(Arg.Any<LinkUpdateRequest>())
            .Returns(Task.FromException<Result>(new Exception("Network fail connection timed out")));

        var bgService = scope.ServiceProvider.GetServices<Microsoft.Extensions.Hosting.IHostedService>()
            .OfType<OutboxBackgroundService>()
            .FirstOrDefault();

        Assert.NotNull(bgService);

        var cts = new CancellationTokenSource();
        var bgTask = bgService.StartAsync(cts.Token);

        await Task.Delay(4000);
        await cts.CancelAsync();

        var dbEvents = await GetEventsAsync();
        Assert.Single(dbEvents);
        Assert.Equal((int)EventStatus.Failed, dbEvents[0].status);
    }
}

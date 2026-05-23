using System.Text.Json;
using Confluent.Kafka;
using LinkTracker.Bot.Contracts.Rest;
using LinkTracker.Scrapper.Application.Dtos.Outbox;
using LinkTracker.Scrapper.Application.Scraping.ApiClients.Bot;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Notifications;
using LinkTracker.Scrapper.Infrastructure.Pipelines.NotificationStrategies;
using LinkTracker.Tests.Scrapper;
using LinkTracker.Tests.Shared;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Polly.CircuitBreaker;
using Testcontainers.Kafka;

namespace LinkTracker.Tests.Interservice.Broker;

public class FallbackTests : IAsyncLifetime
{
    private ScrapperWebApplicationFactory _baseFactory = null!;
    private WebApplicationFactory<LinkTracker.Scrapper.Program> _factory = null!;

    private readonly KafkaContainer _kafka = new KafkaBuilder("apache/kafka:3.8.1").Build();
    private string _topic = "test-notifications";

    private MockHttpMessageHandler _mockHandler = null!;

    public async Task InitializeAsync()
    {
        await _kafka.StartAsync();

        _mockHandler = new MockHttpMessageHandler();
        _mockHandler.SetSendLambda((_, _) =>
            throw new BrokenCircuitException("circuit is open"));

        _baseFactory = new ScrapperWebApplicationFactory();
        await _baseFactory.InitializeAsync();
        _factory = _baseFactory
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("NotificationStrategyOptions:Strategy", ((int)NotificationStrategies.Http).ToString());
                builder.UseSetting("NotificationStrategyOptions:FallbackToKafka", "true");
                builder.UseSetting("NotificationStrategyOptions:BootstrapServers", _kafka.GetBootstrapAddress());
                builder.UseSetting("NotificationStrategyOptions:Topic", _topic);
                builder.UseSetting("TopicConfiguration:NumPartitions", "1");
                builder.UseSetting("TopicConfiguration:ReplicationFactor", "1");

                builder.ConfigureTestServices(services =>
                {
                    var mockBotClient = Substitute.For<IBotClient>();
                    mockBotClient
                        .SendUpdateAsync(Arg.Any<LinkUpdateRequest>())
                        .ThrowsAsync(new BrokenCircuitException("circuit open"));

                    services.AddScoped(_ => mockBotClient);
                });
            });

        _ = _factory.Services;
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _baseFactory.DisposeAsync();
        await _kafka.DisposeAsync();
    }

    [Fact]
    public async Task Fallback_SendsToKafka_WhenHttpCircuitOpen()
    {
        var strategy = _factory.Services.GetRequiredService<INotificationStrategy>();

        var events = new[]
        {
            new OutboxEvent(
                1,
                [],
                JsonSerializer.Serialize(new LinkUpdateRequest(Guid.NewGuid(), "https://some-cool-web.com", "desc", [123], "HIGH")),
                0,
                [],
                EventId.LinkUpdated, EventStatus.Pending, DateTime.UtcNow, DateTime.UtcNow
            )
        };

        var result = await strategy.SendAsync(events, CancellationToken.None);

        Assert.Empty(result.FailedToDelivers);
        Assert.Contains(1L, result.DeliveredIds);

        AssertMessageInKafkaAsync(_topic);
    }

    private void AssertMessageInKafkaAsync(string topic)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _kafka.GetBootstrapAddress(),
            GroupId = "test-consumer",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        consumer.Subscribe(topic);

        var message = consumer.Consume(TimeSpan.FromSeconds(5));
        Assert.NotNull(message?.Message?.Value);
    }
}
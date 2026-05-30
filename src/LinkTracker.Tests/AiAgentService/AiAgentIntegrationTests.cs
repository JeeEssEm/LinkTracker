using System.Text.Json;
using AiAgentService.Web.Infrastructure.ExternalApi;
using Confluent.Kafka;
using LinkTracker.Bot.Contracts.Rest;
using LinkTracker.Scrapper.Contracts.Dtos.Notifications;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.Kafka;
using LinkTracker.Tests.AiAgentService;
using NSubstitute;

namespace LinkTracker.Tests.AiAgentService;

public class AiAgentIntegrationTests : IAsyncLifetime
{
    private readonly KafkaContainer _kafka = new KafkaBuilder("apache/kafka:3.8.1").Build();
    private AiAgentWebApplicationFactory _factory = null!;
    private string _bootstrapServers = null!;

    private readonly string _inputTopic = "link.raw-updates";
    private readonly string _outputTopic = "link.processed-updates";

    public async Task InitializeAsync()
    {
        await _kafka.StartAsync();

        _bootstrapServers = _kafka.GetBootstrapAddress();

        _factory = new AiAgentWebApplicationFactory();
        var configuredFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("KafkaConfiguration:BootstrapServers", _bootstrapServers);
            builder.UseSetting("KafkaConfiguration:ConsumerTopic", _inputTopic);
            builder.UseSetting("KafkaConfiguration:ProducerTopic", _outputTopic);
            builder.UseSetting("KafkaConfiguration:GroupId", "test-ai-group");
            builder.UseSetting("KafkaConfiguration:AutoOffsetReset", ((int)AutoOffsetReset.Earliest).ToString());

            builder.UseSetting("TopicConfiguration:NumPartitions", "1");
            builder.UseSetting("TopicConfiguration:ReplicationFactor", "1");
            builder.UseSetting("TopicConfiguration:InSyncReplicas", "1");

            builder.UseSetting("Filtering:MinLength", "5");
            builder.UseSetting("Filtering:Summarization:Threshold", "500");
            builder.UseSetting("Filtering:Summarization:AiSummarizationEnabled", "false");

            builder.UseSetting("AiApiConfiguration:Url", "https://some-llm-api.com");
            builder.UseSetting("AiApiConfiguration:ApiKey", "");

            builder.UseSetting("Filtering:ExcludedAuthors:0", "unknown");
            builder.UseSetting("Prioritization:HighKeywords:0", "critical");
            builder.UseSetting("Prioritization:AiPrioritizationEnabled", "false");
            builder.UseSetting("Grouping:WindowMs", "2000");
        });

        _ = configuredFactory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _kafka.DisposeAsync();
    }

    [Fact]
    public async Task Test_ProcessValidMessage()
    {
        using var producer = new ProducerBuilder<Null, string>(new ProducerConfig { BootstrapServers = _bootstrapServers, BrokerAddressFamily = BrokerAddressFamily.V4 }).Build();

        var messageData = new UpdateMessage(Guid.NewGuid(), "https://some-api.com", "test_user", "Some valid description that is long enough.", [1L, 2L]);
        var json = JsonSerializer.Serialize(messageData);

        using var consumer = new ConsumerBuilder<Null, string>(new ConsumerConfig { BootstrapServers = _bootstrapServers, GroupId = "test_verifier", AutoOffsetReset = AutoOffsetReset.Earliest, BrokerAddressFamily = BrokerAddressFamily.V4 }).Build();

        consumer.Subscribe(_outputTopic);

        var report = await producer.ProduceAsync(_inputTopic, new Message<Null, string> { Value = json });
        producer.Flush();

        Assert.Equal(PersistenceStatus.Persisted, report.Status);

        var consumeResult = consumer.Consume(TimeSpan.FromSeconds(10));

        Assert.NotNull(consumeResult);
        Assert.Contains("Some valid description that is long enough.", consumeResult.Message.Value);
    }

    [Fact]
    public async Task Test_HandleInvalidMessageFormatWithoutCrash()
    {
        using var producer = new ProducerBuilder<Null, string>(new ProducerConfig { BootstrapServers = _bootstrapServers, BrokerAddressFamily = BrokerAddressFamily.V4 }).Build();

        using var consumer = new ConsumerBuilder<Null, string>(new ConsumerConfig { BootstrapServers = _bootstrapServers, GroupId = "test_verifier_2", AutoOffsetReset = AutoOffsetReset.Earliest, BrokerAddressFamily = BrokerAddressFamily.V4 }).Build();

        consumer.Subscribe(_outputTopic);

        await producer.ProduceAsync(_inputTopic, new Message<Null, string> { Value = "{" });

        var validData = new UpdateMessage(Guid.NewGuid(), "https://some-api.com", "test_user", "Some valid description that is long enough.", [1L, 2L]);

        await producer.ProduceAsync(_inputTopic, new Message<Null, string> { Value = JsonSerializer.Serialize(validData) });
        producer.Flush();

        var consumeResult = consumer.Consume(TimeSpan.FromSeconds(15));

        Assert.NotNull(consumeResult);
        Assert.Contains("Some valid description that is long enough.", consumeResult.Message.Value);
    }

    [Fact]
    public async Task Test_ProcessGroupedMessages()
    {
        using var producer = new ProducerBuilder<Null, string>(new ProducerConfig { BootstrapServers = _bootstrapServers, BrokerAddressFamily = BrokerAddressFamily.V4 }).Build();

        var messageData1 = new UpdateMessage(Guid.NewGuid(), "https://some-api.com", "test_user", "Long description", [1L, 2L]);
        var messageData2 = new UpdateMessage(Guid.NewGuid(), "https://some-api.com", "test_user", "Just description", [1L, 2L]);
        var json1 = JsonSerializer.Serialize(messageData1);
        var json2 = JsonSerializer.Serialize(messageData2);

        using var consumer = new ConsumerBuilder<Null, string>(new ConsumerConfig { BootstrapServers = _bootstrapServers, GroupId = "test_verifier", AutoOffsetReset = AutoOffsetReset.Earliest, BrokerAddressFamily = BrokerAddressFamily.V4 }).Build();

        consumer.Subscribe(_outputTopic);

        var report1 = await producer.ProduceAsync(_inputTopic, new Message<Null, string> { Value = json1 });
        var report2 = await producer.ProduceAsync(_inputTopic, new Message<Null, string> { Value = json2 });
        producer.Flush();

        Assert.Equal(PersistenceStatus.Persisted, report1.Status);
        Assert.Equal(PersistenceStatus.Persisted, report2.Status);

        var consumeResult = consumer.Consume(TimeSpan.FromSeconds(10));

        Assert.NotNull(consumeResult);
        var deserialized = JsonSerializer.Deserialize<LinkUpdateRequest>(consumeResult.Message.Value);
        Assert.NotNull(deserialized);
        Assert.Contains(1L, deserialized.TgChatIds);
        Assert.Contains(2L, deserialized.TgChatIds);
        Assert.Contains(". Long description", deserialized.Description);
        Assert.Contains(". Just description", deserialized.Description);
    }

    [Fact]
    public async Task Test_ProcessFilteredMessages()
    {
        using var producer = new ProducerBuilder<Null, string>(new ProducerConfig { BootstrapServers = _bootstrapServers, BrokerAddressFamily = BrokerAddressFamily.V4 }).Build();

        var messageData = new UpdateMessage(Guid.NewGuid(), "https://some-api.com", "unknown", "Some valid description that is long enough.", [1L, 2L]);
        var json = JsonSerializer.Serialize(messageData);

        using var consumer = new ConsumerBuilder<Null, string>(new ConsumerConfig { BootstrapServers = _bootstrapServers, GroupId = "test_verifier", AutoOffsetReset = AutoOffsetReset.Earliest, BrokerAddressFamily = BrokerAddressFamily.V4 }).Build();

        consumer.Subscribe(_outputTopic);

        var report = await producer.ProduceAsync(_inputTopic, new Message<Null, string> { Value = json });
        producer.Flush();

        Assert.Equal(PersistenceStatus.Persisted, report.Status);

        var consumeResult = consumer.Consume(TimeSpan.FromSeconds(10));

        Assert.Null(consumeResult);
    }
}
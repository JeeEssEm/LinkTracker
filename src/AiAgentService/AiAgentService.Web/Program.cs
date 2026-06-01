using System.Net.Http.Headers;
using AiAgentService.Web.BackgroundServices;
using AiAgentService.Web.Configurations;
using AiAgentService.Web.Infrastructure;
using AiAgentService.Web.Infrastructure.ExternalApi;
using AiAgentService.Web.Infrastructure.MessageAcknowledgment;
using AiAgentService.Web.Metrics;
using AiAgentService.Web.Pipeline;
using AiAgentService.Web.Pipeline.Filters;
using AiAgentService.Web.Pipeline.Filters.Abstractions;
using AiAgentService.Web.Pipeline.Filters.BannedAuthors;
using AiAgentService.Web.Pipeline.Filters.LengthFilter;
using AiAgentService.Web.Pipeline.Filters.StopWords;
using AiAgentService.Web.Pipeline.Handlers.Kafka;
using AiAgentService.Web.Pipeline.Handlers.Prioritizing;
using AiAgentService.Web.Pipeline.Handlers.Summarization;
using Confluent.Kafka;
using LinkTracker.Contracts.Broker;
using OpenTelemetry.Metrics;
using Refit;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<TopicConfiguration>(builder.Configuration.GetSection(TopicConfiguration.Position));
builder.Services.Configure<KafkaConfiguration>(builder.Configuration.GetSection(KafkaConfiguration.Position));
builder.Services.Configure<FiltersConfiguration>(builder.Configuration.GetSection(FiltersConfiguration.Position));
builder.Services.Configure<AiApiConfiguration>(builder.Configuration.GetSection(AiApiConfiguration.Position));
builder.Services.Configure<PrioritizationConfiguration>(builder.Configuration.GetSection(PrioritizationConfiguration.Position));
builder.Services.Configure<GroupingConfiguration>(builder.Configuration.GetSection(GroupingConfiguration.Position));

var filtersConfig = builder.Configuration
    .GetSection(FiltersConfiguration.Position)
    .Get<FiltersConfiguration>();

if (filtersConfig == null)
{
    throw new ArgumentException("Configuration for filters is not set!");
}

builder.Services.AddSingleton<IMessageFilter, BannedAuthorsFilter>();
builder.Services.AddSingleton<IMessageFilter, StopWordsFilter>();
builder.Services.AddSingleton<IMessageFilter, LengthFilter>();
builder.Services.AddSingleton<AggregateFilter>();

builder.Services.AddScoped<SummarizationHandler>();
builder.Services.AddScoped<PrioritizingHandler>();

builder.Services.AddSingleton<ISendToKafkaHandler, SendToKafkaHandler>();
builder.Services.AddSingleton<MessageProcessingPipeline>();

builder.Services.AddSingleton<MessageAcknowledgementManager>();
builder.Services.AddSingleton<IConsumerUpdatesManager, ConsumerUpdatesManager>();


var topicConfig = builder.Configuration
    .GetSection(TopicConfiguration.Position)
    .Get<TopicConfiguration>();
if (topicConfig == null)
{
    throw new ArgumentException("Topic configuration is not set!");
}
var kafkaConfig = builder.Configuration
    .GetSection(KafkaConfiguration.Position)
    .Get<KafkaConfiguration>();
if (kafkaConfig == null)
{
    throw new ArgumentException("Config for connection to kafka is not set!");
}

await KafkaBrokerManager.EnsureTopicExistsAsync(topicConfig, kafkaConfig.ProducerTopic, kafkaConfig.BootstrapServers);
await KafkaBrokerManager.EnsureTopicExistsAsync(topicConfig, kafkaConfig.ConsumerTopic, kafkaConfig.BootstrapServers);

builder.Services.AddSingleton<IProducer<Null, string>>(_ =>
{
    var config = new ProducerConfig
    {
        BootstrapServers = kafkaConfig.BootstrapServers,
        Acks = kafkaConfig.Acks,
        BrokerAddressFamily = BrokerAddressFamily.V4
    };
    return new ProducerBuilder<Null, string>(config).Build();
});

builder.Services.AddSingleton<ConsumerConfig>(_ => new ConsumerConfig
{
    BootstrapServers = kafkaConfig.BootstrapServers,
    GroupId = kafkaConfig.GroupId,
    AutoOffsetReset = kafkaConfig.AutoOffsetReset,
    BrokerAddressFamily = BrokerAddressFamily.V4,
});

builder.Services.AddHostedService<UpdateMessagesConsumerService>();

var aiConfig = builder.Configuration
    .GetSection(AiApiConfiguration.Position)
    .Get<AiApiConfiguration>();
if (aiConfig == null)
{
    throw new ArgumentException("Configuration for AI model is not set!");
}

builder.Services
    .AddRefitClient<IOpenAiApiClient>()
    .ConfigureHttpClient(c =>
    {
        c.BaseAddress = new Uri(aiConfig.Url);
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", aiConfig.ApiKey);
    })
    .AddStandardResilienceHandler();

builder.Services.AddSingleton<ApplicationMetrics>();
builder.Services.AddScoped<IOpenAiCompatibleApiClient, OpenAiClientWrapper>();
builder.Services.AddOpenTelemetry()
    .WithMetrics(m =>
        m
            .AddAspNetCoreInstrumentation()
            .AddRuntimeInstrumentation()
            .AddMeter("AiAgentService.Web.Metrics.ApplicationMetrics")
            .AddPrometheusExporter()
    );
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapPrometheusScrapingEndpoint();

app.Run();

using System.Text.Json;
using Confluent.Kafka;
using LinkTracker.Bot.App;
using LinkTracker.Bot.App.ApiClients.Scrapper;
using LinkTracker.Bot.App.ApiClients.Scrapper.Grpc;
using LinkTracker.Bot.App.ApiClients.Scrapper.Rest;
using LinkTracker.Bot.App.BackgroundServices.NotifierService;
using LinkTracker.Bot.App.Features.Api;
using LinkTracker.Bot.App.Features.Api.Updates;
using LinkTracker.Bot.App.Features.Bot.TrackLink.Flow;
using LinkTracker.Bot.App.Infrastructure.Middlewares;
using LinkTracker.Bot.App.StateController;
using LinkTracker.Bot.Contracts.GrpcErrorProfiles;
using LinkTracker.Bot.Core.DependencyInjectionExtensions;
using LinkTracker.Bot.Core.Middlewares;
using LinkTracker.Bot.Core.StateMachine.Abstractions;
using LinkTracker.Contracts.Broker;
using LinkTracker.Contracts.GrpcErrors;
using LinkTracker.Scrapper.Contracts.GrpcErrorProfiles;
using LinkTracker.Scrapper.Grpc.Chats;
using LinkTracker.Scrapper.Grpc.Links;
using Microsoft.Extensions.Options;
using Refit;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<BotOptions>(builder.Configuration.GetSection(BotOptions.Position));
builder.Services.Configure<ScrapperClientOptions>(builder.Configuration.GetSection(ScrapperClientOptions.Position));
builder.Services.Configure<KafkaConsumerOptions>(builder.Configuration.GetSection(KafkaConsumerOptions.Position));
builder.Services.Configure<TopicConfiguration>(builder.Configuration.GetSection(TopicConfiguration.Position));

builder.Services
    .AddHttpClient("telegram_bot_client")
    .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
    {
        var botConfig = sp.GetRequiredService<IOptions<BotOptions>>().Value;
        return new TelegramBotClient(botConfig.Token, httpClient);
    });
builder.Services.AddScoped<LinkUpdatesHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();

builder.Services.AddSingleton<IStateController, InMemoryStateController>();
builder.Services.AddScoped<LinkAdditionHandler>();

builder.Services.AddTelegramBotPipeline(registry =>
{
    registry.RegisterMiddleware<StateMiddleware>();
    registry.UseDefaultDispatcher();
});

builder.Services.AddTelegramBotDependencies();
builder.Services.AddTelegramNotifier();

builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.JsonWriterOptions = new JsonWriterOptions() { Indented = true };
});
builder.Logging.AddSeq(builder.Configuration.GetSection("Seq"));

var scrapperOptions = builder.Configuration
    .GetSection(ScrapperClientOptions.Position)
    .Get<ScrapperClientOptions>();
if (scrapperOptions == null)
{
    throw new ArgumentException("Scrapper options are not configured! See ScrapperClientOptions.cs for required parameters");
}

builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();
builder.Services.AddGrpcErrorMapping(typeof(UpdatesGrpcErrorsProfile).Assembly, typeof(ChatsGrpcErrorsProfile).Assembly);

if (scrapperOptions.UseGrpc)
{
    builder.Services.AddGrpcClient<ScrapperChatsService.ScrapperChatsServiceClient>(o => o.Address = new Uri(scrapperOptions.BaseUrl));
    builder.Services.AddGrpcClient<ScrapperLinksService.ScrapperLinksServiceClient>(o => o.Address = new Uri(scrapperOptions.BaseUrl));

    builder.Services.AddScoped<IScrapperClient, ScrapperGrpcAdapter>();
}
else
{
    builder.Services
        .AddRefitClient<IScrapperRefitClient>()
        .ConfigureHttpClient((sp, c) =>
        {
            var scrapperConfig = sp.GetRequiredService<IOptions<ScrapperClientOptions>>().Value;
            c.BaseAddress = new Uri(scrapperConfig.BaseUrl);
        });
    builder.Services.AddScoped<IScrapperClient, ScrapperRestAdapter>();
}

var consumerOptions = builder.Configuration
    .GetSection(KafkaConsumerOptions.Position)
    .Get<KafkaConsumerOptions>();
if (consumerOptions == null)
{
    throw new ArgumentException("Kafka consumer options are not set!");
}
if (consumerOptions.KafkaConsumerEnabled)
{
    builder.Services.AddSingleton<ConsumerConfig>(_ => new ConsumerConfig()
    {
        BootstrapServers = consumerOptions.BootstrapServers,
        GroupId = consumerOptions.GroupId,
        AutoOffsetReset = AutoOffsetReset.Latest,
        BrokerAddressFamily = BrokerAddressFamily.V4
    });
    builder.Services.AddHostedService<ConsumerNotifierService>();
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    if (scrapperOptions.UseGrpc)
    {
        app.MapGrpcReflectionService();
    }

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGrpcService<BotGrpcService>();

app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
app.MapUpdates();

app.Run();

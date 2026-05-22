using System.Text.Json;
using Confluent.Kafka;
using LinkTracker.Bot.Contracts.GrpcErrorProfiles;
using LinkTracker.Bot.Grpc.Links;
using LinkTracker.Contracts.Broker;
using LinkTracker.Contracts.GrpcErrors;
using LinkTracker.Scrapper.Application.Handlers.Chats;
using LinkTracker.Scrapper.Application.Handlers.Links;
using LinkTracker.Scrapper.Application.QueryServices;
using LinkTracker.Scrapper.Application.Scraping.ApiClients.Bot;
using LinkTracker.Scrapper.Application.Scraping.ApiClients.Github;
using LinkTracker.Scrapper.Application.Scraping.ApiClients.StackOverflow;
using LinkTracker.Scrapper.Application.Scraping.Pipelines;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Github;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Steps;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Notifications;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Steps;
using LinkTracker.Scrapper.Configurations;
using LinkTracker.Scrapper.Contracts.GrpcErrorProfiles;
using LinkTracker.Scrapper.Domain.Tracking.Repositories;
using LinkTracker.Scrapper.Domain.Tracking.Services;
using LinkTracker.Scrapper.Domain.Tracking.SourceHandlers;
using LinkTracker.Scrapper.Endpoints.Chats;
using LinkTracker.Scrapper.Endpoints.Chats.AddChat;
using LinkTracker.Scrapper.Endpoints.Chats.RemoveChat;
using LinkTracker.Scrapper.Endpoints.Links;
using LinkTracker.Scrapper.Endpoints.Links.AddLink;
using LinkTracker.Scrapper.Endpoints.Links.GetLinks;
using LinkTracker.Scrapper.Endpoints.Links.RemoveLink;
using LinkTracker.Scrapper.Extensions;
using LinkTracker.Scrapper.Infrastructure.Middleware;
using LinkTracker.Scrapper.Infrastructure.Orm;
using LinkTracker.Scrapper.Infrastructure.Pipelines.BulkServices;
using LinkTracker.Scrapper.Infrastructure.Pipelines.Cache;
using LinkTracker.Scrapper.Infrastructure.Pipelines.Cache.CachedQueryServices;
using LinkTracker.Scrapper.Infrastructure.Pipelines.Cache.CachedRepositories;
using LinkTracker.Scrapper.Infrastructure.Pipelines.Database;
using LinkTracker.Scrapper.Infrastructure.Pipelines.ExternalApi.BotNotifier.Grpc;
using LinkTracker.Scrapper.Infrastructure.Pipelines.ExternalApi.BotNotifier.Rest;
using LinkTracker.Scrapper.Infrastructure.Pipelines.ExternalApi.Github;
using LinkTracker.Scrapper.Infrastructure.Pipelines.ExternalApi.StackOverflow;
using LinkTracker.Scrapper.Infrastructure.Pipelines.NotificationStrategies;
using LinkTracker.Scrapper.Infrastructure.Pipelines.Outbox;
using LinkTracker.Scrapper.Infrastructure.Sql;
using LinkTracker.Scrapper.Scheduling;
using Quartz;
using Refit;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);
using var loggerFactory = LoggerFactory.Create(loggingBuilder =>
{
    loggingBuilder.AddConsole();
});
var logger = loggerFactory.CreateLogger<Program>();

builder.Services.Configure<SchedulerOptions>(builder.Configuration.GetSection(SchedulerOptions.Position));
builder.Services.Configure<BotClientOptions>(builder.Configuration.GetSection(BotClientOptions.Position));
builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection(DatabaseOptions.Position));
builder.Services.Configure<StackOverflowPipelineOptions>(builder.Configuration.GetSection(StackOverflowPipelineOptions.Position));
builder.Services.Configure<GithubPipelineOptions>(builder.Configuration.GetSection(GithubPipelineOptions.Position));
builder.Services.Configure<NotificationStrategyOptions>(builder.Configuration.GetSection(NotificationStrategyOptions.Position));
builder.Services.Configure<TopicConfiguration>(builder.Configuration.GetSection(TopicConfiguration.Position));
builder.Services.Configure<OutboxConfiguration>(builder.Configuration.GetSection(OutboxConfiguration.Position));
builder.Services.Configure<CacheConfiguration>(builder.Configuration.GetSection(CacheConfiguration.Position));
builder.Services.Configure<ResilienceOptions>(builder.Configuration.GetSection(ResilienceOptions.Position));

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<LinkManager>();
builder.Services.AddLinkSourceHandlers();

builder.Services.AddScoped<AddChatHandler>();
builder.Services.AddScoped<RemoveChatHandler>();
builder.Services.AddScoped<AddLinkHandler>();
builder.Services.AddScoped<RemoveLinkHandler>();
builder.Services.AddScoped<GetLinksHandler>();

var resilienceOptions = builder.Configuration
    .GetSection(ResilienceOptions.Position)
    .Get<ResilienceOptions>();
if (resilienceOptions == null)
{
    logger.LogWarning("Options for fault tolerance are not set. Using default");
    resilienceOptions = new ResilienceOptions();
}

builder.Services.AddPerIpRateLimiting(resilienceOptions.RateLimitOptions);

builder.Services
    .AddRefitClient<IGithubRefitClient>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.github.com"))
    .AddResilienceHandler("github", b => b.BuildPipeline(resilienceOptions.GithubClientOptions));

builder.Services
    .AddRefitClient<IStackOverflowRefitClient>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.stackexchange.com"))
    .AddResilienceHandler("stackoverflow", b => b.BuildPipeline(resilienceOptions.StackOverflowClientOptions));

builder.Services.AddScoped<IStackOverflowClient, StackOverflowClient>();
builder.Services.AddScoped<IGithubClient, GithubClient>();

var databaseOptions = builder.Configuration
    .GetSection(DatabaseOptions.Position)
    .Get<DatabaseOptions>();
if (databaseOptions == null)
{
    throw new ArgumentException("Options for database are not provided");
}

builder.Services.AddCommonDatabaseDependencies(databaseOptions.ConnectionString);
builder.Services.AddBulkServices();
if (databaseOptions.AccessType.Equals("ORM", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddOrmContext();
    builder.Services.AddOrmRepositories();
}
else if (databaseOptions.AccessType.Equals("SQL", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSqlRepositories();
}
else
{
    throw new ArgumentException($"database access type is not recognized! Possible values: ORM, SQL. Received: {databaseOptions.AccessType}");
}

var botClientOptions = builder.Configuration
    .GetSection(BotClientOptions.Position)
    .Get<BotClientOptions>();
if (botClientOptions == null)
{
    throw new ArgumentException("Options for bot client are not provided! See BotClientOptions.cs for required parameters");
}

builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();
builder.Services.AddGrpcErrorMapping(typeof(ChatsGrpcErrorsProfile).Assembly, typeof(UpdatesGrpcErrorsProfile).Assembly);

if (botClientOptions.UseGrpc)
{
    builder.Services.AddGrpcClient<BotService.BotServiceClient>(o => o.Address = new Uri(botClientOptions.BaseUrl));
    builder.Services.AddScoped<IBotClient, BotGrpcAdapter>();
}
else
{
    builder.Services
        .AddRefitClient<IBotRefitClient>()
        .ConfigureHttpClient(c => c.BaseAddress = new Uri(botClientOptions.BaseUrl))
        .AddResilienceHandler("bot", b => b.BuildPipeline(resilienceOptions.BotClientOptions));
    builder.Services.AddScoped<IBotClient, BotRestAdapter>();
}


var notificationStrategyOptions = builder.Configuration
    .GetSection(NotificationStrategyOptions.Position)
    .Get<NotificationStrategyOptions>();
if (notificationStrategyOptions == null)
{
    throw new ArgumentException("Options for notification strategy are not provided!");
}
else
{
    var topicConfig = builder.Configuration
        .GetSection(TopicConfiguration.Position)
        .Get<TopicConfiguration>() ?? new TopicConfiguration();

    switch (notificationStrategyOptions.Strategy)
    {
        case NotificationStrategies.Kafka:
            await builder.Services.PrepareAsyncNotificationStrategy(topicConfig, notificationStrategyOptions);

            builder.Services.AddSingleton<INotificationStrategy, AsyncNotificationStrategy>();
            break;
        case NotificationStrategies.Grpc:
            if (!botClientOptions.UseGrpc)
            {
                throw new ArgumentException("Cannot init notifications via gRPC if BotClientOptions.UseGrpc = false");
            }

            builder.Services.AddScoped<INotificationStrategy, SyncNotificationStrategy>();
            break;
        case NotificationStrategies.Http:
            if (botClientOptions.UseGrpc)
            {
                throw new ArgumentException("Cannot init notifications via HTTP if BotClientOptions.UseGrpc = true");
            }
            builder.Services.AddScoped<INotificationStrategy, SyncNotificationStrategy>();

            if (notificationStrategyOptions.FallbackToKafka)
            {
                await builder.Services.PrepareAsyncNotificationStrategy(topicConfig, notificationStrategyOptions);

                builder.Services.AddSingleton<AsyncNotificationStrategy>();
                builder.Services.AddScoped<SyncNotificationStrategy>();

                builder.Services.AddScoped<INotificationStrategy, FallbackNotificationStrategy>();
            }
            else
            {
                builder.Services.AddScoped<INotificationStrategy, SyncNotificationStrategy>();
            }
            break;
    }
}

var schedulerOptions = builder.Configuration
    .GetSection(SchedulerOptions.Position)
    .Get<SchedulerOptions>();

builder.Services.AddSingleton<StackOverflowPipeline>();
builder.Services.AddSingleton<FetchAnswersStep>();
builder.Services.AddSingleton<ClearAnswersStep>();
builder.Services.AddSingleton<FetchQuestionsStep>();
builder.Services.AddSingleton<ClearQuestionsStep>();
builder.Services.AddSingleton<ProcessAnswersStep>();
builder.Services.AddSingleton<ProcessCommentsStep>();
builder.Services.AddSingleton<FetchCommentsStep>();
builder.Services.AddSingleton<FetchQuestionCommentsStep>();
builder.Services.AddSingleton<ProcessQuestionCommentsStep>();

builder.Services.AddSingleton<GithubPipeline>();
builder.Services.AddSingleton<FetchIssuesStep>();
builder.Services.AddSingleton<FetchPullRequestsStep>();
builder.Services.AddSingleton<ProcessDeletedRepositoriesStep>();
builder.Services.AddSingleton<ProcessIssuesStep>();
builder.Services.AddSingleton<ProcessPullRequestsStep>();

builder.Services.AddSingleton<ScrapingCoordinator>();

builder.Services.AddQuartz(q =>
{
    var jobKey = new JobKey("LinkCheckJob");
    q.AddJob<LinkCheckJob>(opts => opts.WithIdentity(jobKey));
    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("LinkCheckJob-trigger")
        .WithSimpleSchedule(x => x
            .WithIntervalInMinutes(schedulerOptions?.CheckIntervalInMinutes ?? 5)
            .RepeatForever()));
});
builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

builder.Services.AddHostedService<OutboxBackgroundService>();

var cacheOptions = builder.Configuration
    .GetSection(CacheConfiguration.Position)
    .Get<CacheConfiguration>();
if (cacheOptions != null && cacheOptions.CacheEnabled)
{
    cacheOptions.Validate();
    builder.Services.AddScoped<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(cacheOptions.ConnectionString!));

    builder.Services.Decorate<ISubscriptionQueryService, CachedSubscriptionQueryService>();
    builder.Services.Decorate<ISubscriptionRepository, CachedSubscriptionRepository>();
    builder.Services.Decorate<IChatRepository, CachedChatRepository>();
}

builder.Logging.AddSeq(builder.Configuration.GetSection("Seq"));
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.JsonWriterOptions = new JsonWriterOptions { Indented = true };
});

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    if (botClientOptions.UseGrpc)
    {
        app.MapGrpcReflectionService();
    }

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseHttpsRedirection();

app.UseRateLimiter();
app.MapAddChat();
app.MapRemoveChat();
app.MapAddLink();
app.MapRemoveLink();
app.MapGetLinks();

app.MapGrpcService<LinksGrpcService>();
app.MapGrpcService<ChatsGrpcService>();

app.Run();
using System.Text.Json;
using LinkTracker.Bot.Contracts.GrpcErrorProfiles;
using LinkTracker.Bot.Grpc.Links;
using LinkTracker.Contracts.GrpcErrors;
using LinkTracker.Scrapper;
using LinkTracker.Scrapper.ApiClients;
using LinkTracker.Scrapper.ApiClients.Abstractions;
using LinkTracker.Scrapper.ApiClients.BotNotifier;
using LinkTracker.Scrapper.ApiClients.BotNotifier.Grpc;
using LinkTracker.Scrapper.ApiClients.BotNotifier.Rest;
using LinkTracker.Scrapper.ApiClients.Resources.Github;
using LinkTracker.Scrapper.ApiClients.Resources.Github.Repository;
using LinkTracker.Scrapper.ApiClients.Resources.StackOverflow;
using LinkTracker.Scrapper.ApiClients.Resources.StackOverflow.Question;
using LinkTracker.Scrapper.Application.Repositories;
using LinkTracker.Scrapper.Configurations;
using LinkTracker.Scrapper.Contracts.GrpcErrorProfiles;
using LinkTracker.Scrapper.Features.Chats;
using LinkTracker.Scrapper.Features.Chats.AddChat;
using LinkTracker.Scrapper.Features.Chats.RemoveChat;
using LinkTracker.Scrapper.Features.Links;
using LinkTracker.Scrapper.Features.Links.AddLink;
using LinkTracker.Scrapper.Features.Links.GetLinks;
using LinkTracker.Scrapper.Features.Links.RemoveLink;
using LinkTracker.Scrapper.Infrastructure.Middleware;
using LinkTracker.Scrapper.Infrastructure.Migrations;
using LinkTracker.Scrapper.Infrastructure.Orm;
using LinkTracker.Scrapper.Infrastructure.Sql;
using LinkTracker.Scrapper.Scheduling;
using Quartz;
using Refit;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<SchedulerOptions>(builder.Configuration.GetSection(SchedulerOptions.Position));
builder.Services.Configure<BotClientOptions>(builder.Configuration.GetSection(BotClientOptions.Position));
builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection(DatabaseOptions.Position));

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<AddChatHandler>();
builder.Services.AddScoped<RemoveChatHandler>();
builder.Services.AddScoped<AddLinkHandler>();
builder.Services.AddScoped<RemoveLinkHandler>();
builder.Services.AddScoped<GetLinksHandler>();

builder.Services
    .AddRefitClient<IGithubClient>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.github.com"));

builder.Services
    .AddRefitClient<IStackOverflowClient>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.stackexchange.com"));

var databaseOptions = builder.Configuration
    .GetSection(DatabaseOptions.Position)
    .Get<DatabaseOptions>();
if (databaseOptions == null)
{
    throw new ArgumentException("Options for database are not provided");
}

if (databaseOptions.AccessType.Equals("ORM", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddOrmDatabase(databaseOptions.ConnectionString);
    builder.Services.AddOrmRepositories();
}
else if (databaseOptions.AccessType.Equals("SQL", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSqlDatabase(databaseOptions.ConnectionString);
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
        .ConfigureHttpClient(c => c.BaseAddress = new Uri(botClientOptions.BaseUrl));
    builder.Services.AddScoped<IBotClient, BotRestAdapter>();
}

builder.Services.AddScoped<ILinkChecker, GithubRepositoryChecker>();
builder.Services.AddScoped<ILinkChecker, StackOverflowQuestionChecker>();
builder.Services.AddScoped<LinkCheckerResolver>();

var schedulerOptions = builder.Configuration
    .GetSection(SchedulerOptions.Position)
    .Get<SchedulerOptions>();

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

app.MapAddChat();
app.MapRemoveChat();
app.MapAddLink();
app.MapRemoveLink();
app.MapGetLinks();

app.MapGrpcService<LinksGrpcService>();
app.MapGrpcService<ChatsGrpcService>();

app.Run();
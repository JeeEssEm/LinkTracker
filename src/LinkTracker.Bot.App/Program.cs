using System.Text.Json;
using LinkTracker.Bot.App;
using LinkTracker.Bot.App.ApiClients;
using LinkTracker.Bot.App.ApiClients.Scrapper;
using LinkTracker.Bot.App.ApiClients.Scrapper.Grpc;
using LinkTracker.Bot.App.ApiClients.Scrapper.Rest;
using LinkTracker.Bot.App.Features.Api;
using LinkTracker.Bot.App.Features.Api.Updates;
using LinkTracker.Bot.App.Features.Bot.TrackLink.Flow;
using LinkTracker.Bot.App.StateController;
using LinkTracker.Bot.Core.DependencyInjectionExtensions;
using LinkTracker.Bot.Core.Middlewares;
using LinkTracker.Bot.Core.StateMachine.Abstractions;
using LinkTracker.Contracts.GrpcErrors;
using LinkTracker.Scrapper.Features.Chats.Grpc;
using LinkTracker.Scrapper.Features.Links.Grpc;
using Microsoft.Extensions.Options;
using Refit;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<BotOptions>(builder.Configuration.GetSection(BotOptions.Position));
builder.Services.Configure<ScrapperClientOptions>(builder.Configuration.GetSection(ScrapperClientOptions.Position));

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

builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.JsonWriterOptions = new JsonWriterOptions() { Indented = true };
});

var scrapperOptions = builder.Configuration
    .GetSection(ScrapperClientOptions.Position)
    .Get<ScrapperClientOptions>();
if (scrapperOptions == null)
{
    throw new ArgumentException("Scrapper options are not configured! See ScrapperClientOptions.cs for required parameters");
}

builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();
builder.Services.AddGrpcErrorMapping(typeof(Program).Assembly);

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

app.MapUpdates();

app.Run();

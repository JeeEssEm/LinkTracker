using LinkTracker.Bot.Core.BackgroundServices;
using LinkTracker.Bot.Core.Dispatchers;
using LinkTracker.Bot.Core.StateMachine;
using LinkTracker.Bot.Core.StateMachine.Abstractions;

namespace LinkTracker.Bot.Core.DependencyInjectionExtensions;

public static class TelegramBotCoreDependencyInjectionExtensions
{
    public static IServiceCollection AddTelegramBotDependencies(this IServiceCollection services)
    {
        // dispatchers
        services.AddScoped<ActionDispatcher>();
        services.AddScoped<CommandDispatcher>();
        services.AddScoped<CallbackQueryDispatcher>();
        services.AddScoped<MessageDispatcher>();

        services.AddScoped<UserSession>();

        // handlers
        services.AddTelegramBotCommandDependencies();
        services.AddTelegramBotCallbackQueryDependencies();
        services.AddTelegramBotMessageHandlersDependencies();

        // background service
        services.AddHostedService<TelegramBotBackgroundService>();

        services.AddSingleton<IStateDataStore, StateDataStore>();

        return services;
    }
}
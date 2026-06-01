using LinkTracker.Bot.Core.BackgroundServices;
using LinkTracker.Bot.Core.Dispatchers;
using LinkTracker.Bot.Core.Registries;
using LinkTracker.Bot.Core.Registries.Descriptors;
using LinkTracker.Bot.Core.StateMachine;
using LinkTracker.Bot.Core.StateMachine.Abstractions;
using Telegram.Bot;

namespace LinkTracker.Bot.Core.DependencyInjectionExtensions;

public static class TelegramBotCoreDependencyInjectionExtensions
{
    public static IServiceCollection AddTelegramBotDependencies(this IServiceCollection services, Func<IServiceProvider, Action<CommandDescriptor?>>? actionAfterDispatchingCommandFactory = null)
    {
        // dispatchers
        services.AddScoped<ActionDispatcher>();
        services.AddScoped(sp =>
        {
            var cmdRegistry = sp.GetRequiredService<CommandRegistry>();
            var bot = sp.GetRequiredService<ITelegramBotClient>();
            var logger = sp.GetRequiredService<ILogger<CommandDispatcher>>();
            var userSession = sp.GetRequiredService<UserSession>();
            var action = actionAfterDispatchingCommandFactory?.Invoke(sp);
            return new CommandDispatcher(cmdRegistry, sp, bot, logger, userSession, action);
        });
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
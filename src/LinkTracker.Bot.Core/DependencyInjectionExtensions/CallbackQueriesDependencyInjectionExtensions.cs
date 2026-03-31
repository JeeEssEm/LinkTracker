using System.Reflection;
using LinkTracker.Bot.Core.HandlerTypes.Attributes;
using LinkTracker.Bot.Core.Registries;

namespace LinkTracker.Bot.Core.DependencyInjectionExtensions;

public static class CallbackQueriesDependencyInjectionExtensions
{
    public static IServiceCollection AddTelegramBotCallbackQueryDependencies(this IServiceCollection services)
    {
        var callbackQueryRegistry = new CallbackQueryRegistry();
        services.AddSingleton(callbackQueryRegistry);

        RegisterCallbackQueries(callbackQueryRegistry, services);

        return services;
    }

    private static void RegisterCallbackQueries(CallbackQueryRegistry callbackQueryRegistry, IServiceCollection services)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            foreach (var t in assembly.GetTypes())
            {
                if (t.GetCustomAttribute<BotCallbackQueryAttribute>() != null)
                {
                    services.AddTransient(t);
                    callbackQueryRegistry.RegisterCallbackQuery(t);
                }
            }
        }
    }
}
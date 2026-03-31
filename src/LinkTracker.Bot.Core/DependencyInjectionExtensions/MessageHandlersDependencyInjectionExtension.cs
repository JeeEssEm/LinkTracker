using System.Reflection;
using LinkTracker.Bot.Core.HandlerTypes.Attributes;
using LinkTracker.Bot.Core.Registries;

namespace LinkTracker.Bot.Core.DependencyInjectionExtensions;

public static class MessageHandlersDependencyInjectionExtension
{
    public static IServiceCollection AddTelegramBotMessageHandlersDependencies(this IServiceCollection services)
    {
        var messageHandlersRegistry = new MessageHandlersRegistry();
        services.AddSingleton(messageHandlersRegistry);

        RegisterMessageHandlers(messageHandlersRegistry, services);

        return services;
    }

    private static void RegisterMessageHandlers(MessageHandlersRegistry messageHandlersRegistry, IServiceCollection services)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            foreach (var t in assembly.GetTypes())
            {
                if (t.GetCustomAttribute<UpdateHandlerAttribute>() != null)
                {
                    services.AddTransient(t);
                    messageHandlersRegistry.RegisterHandler(t);
                }
            }
        }
    }
}
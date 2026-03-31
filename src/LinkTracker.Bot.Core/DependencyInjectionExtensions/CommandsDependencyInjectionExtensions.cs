using System.Reflection;
using LinkTracker.Bot.Core.HandlerTypes.Attributes;
using LinkTracker.Bot.Core.Registries;

namespace LinkTracker.Bot.Core.DependencyInjectionExtensions;

public static class CommandsDependencyInjectionExtensions
{
    public static IServiceCollection AddTelegramBotCommandDependencies(this IServiceCollection services)
    {
        var commandRegistry = new CommandRegistry();
        services.AddSingleton(commandRegistry);

        RegisterCommands(commandRegistry, services);

        return services;
    }

    private static void RegisterCommands(CommandRegistry commandRegistry, IServiceCollection services)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            foreach (var t in assembly.GetTypes())
            {
                if (t.GetCustomAttribute<BotCommandAttribute>() != null)
                {
                    services.AddTransient(t);
                    commandRegistry.RegisterCommand(t);
                }
            }
        }
    }
}
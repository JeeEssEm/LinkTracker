using System.Threading.Channels;

namespace LinkTracker.Bot.App.BackgroundServices.NotifierService;

public static class DependencyInjectionNotifierExtension
{
    public static IServiceCollection AddTelegramNotifier(this IServiceCollection services)
    {
        services.AddHostedService<TelegramNotifierService>();

        services.AddSingleton(Channel.CreateUnbounded<NotifyEntry>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = false }
        ));
        services.AddSingleton(sp => sp.GetRequiredService<Channel<NotifyEntry>>().Reader);
        services.AddSingleton(sp => sp.GetRequiredService<Channel<NotifyEntry>>().Writer);

        return services;
    }
}
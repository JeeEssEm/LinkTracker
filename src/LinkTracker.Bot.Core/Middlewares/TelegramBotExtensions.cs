using LinkTracker.Bot.Core.Dispatchers;
using LinkTracker.Bot.Core.Middlewares;
using LinkTracker.Bot.Core.Registries;

namespace LinkTracker.Bot.Core.Middlewares;

public static class TelegramBotExtensions
{
    public static IServiceCollection AddTelegramBotPipeline(this IServiceCollection services, Action<MiddlewareRegistry> configure)
    {
        var registry = new MiddlewareRegistry(services);
        configure(registry);

        services.AddSingleton(registry);

        services.AddSingleton<UpdateDelegate>(sp =>
        {
            var localRegistry = sp.GetRequiredService<MiddlewareRegistry>();

            UpdateDelegate pipeline = localRegistry.TerminalHandler;
            for (int i = localRegistry.Middlewares.Count - 1; i >= 0; i--)
            {
                var factory = localRegistry.Middlewares[i];
                var internalDelegate = factory(sp, pipeline);

                pipeline = (ctx) => internalDelegate(ctx, ctx.Services);
            }

            return pipeline;
        });

        return services;
    }
}
using LinkTracker.Scrapper.Domain.Tracking.Abstractions;
using LinkTracker.Scrapper.Domain.Tracking.SourceHandlers.Github;
using LinkTracker.Scrapper.Domain.Tracking.SourceHandlers.StackOverflow;
using Microsoft.Extensions.DependencyInjection;

namespace LinkTracker.Scrapper.Domain.Tracking.SourceHandlers;

public static class DependencyInjectionSourceHandlersExtension
{
    public static IServiceCollection AddLinkSourceHandlers(this IServiceCollection services)
    {
        services.AddScoped<ILinkSourceHandler, GithubSourceHandler>();
        services.AddScoped<ILinkSourceHandler, StackOverflowSourceHandler>();
        return services;
    }
}

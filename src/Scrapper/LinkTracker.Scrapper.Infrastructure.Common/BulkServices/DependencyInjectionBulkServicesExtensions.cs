using LinkTracker.Scrapper.Application.Repositories;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Store;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Store;
using Microsoft.Extensions.DependencyInjection;

namespace LinkTracker.Scrapper.Infrastructure.Pipelines.BulkServices;

public static class DependencyInjectionBulkServicesExtensions
{
    public static IServiceCollection AddBulkServices(this IServiceCollection services)
    {
        services.AddScoped<ILinkBulkRepository, LinkBulkService>();
        services.AddScoped<IGithubStore, GithubStore>();
        services.AddScoped<IStackOverflowStore, StackOverflowStore>();

        return services;
    }
}
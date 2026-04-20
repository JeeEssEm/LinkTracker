using LinkTracker.Scrapper.Application.Abstractions;
using LinkTracker.Scrapper.Application.Repositories;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Store;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Store;
using LinkTracker.Scrapper.Infrastructure.Sql.DatabaseConnectionFactory;
using LinkTracker.Scrapper.Infrastructure.Sql.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace LinkTracker.Scrapper.Infrastructure.Sql;

public static class DependencyInjectionDatabaseExtension
{
    public static IServiceCollection AddSqlDatabase(this IServiceCollection services, string connectionString)
    {
        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

        services.AddSingleton<IDatabaseConnectionFactory>(_ => new PostgresConnectionFactory(connectionString));
        services.AddScoped<DatabaseSession>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    public static IServiceCollection AddSqlRepositories(this IServiceCollection services)
    {
        services.AddScoped<IChatRepository, ChatRepository>();
        services.AddScoped<ILinkRepository, LinkRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<IStackOverflowStore, SqlStackOverflowStore>();
        services.AddScoped<IGithubStore, SqlGithubStore>();

        return services;
    }
}

using LinkTracker.Scrapper.Application.Abstractions.UnitOfWork;
using LinkTracker.Scrapper.Infrastructure.Pipelines.Database.DatabaseConnectionFactory;
using LinkTracker.Scrapper.Infrastructure.Pipelines.Database.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;

namespace LinkTracker.Scrapper.Infrastructure.Pipelines.Database;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddCommonDatabaseDependencies(this IServiceCollection services, string connectionString)
    {
        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

        services.AddSingleton<IDatabaseConnectionFactory>(_ => new PostgresConnectionFactory(connectionString));
        services.AddScoped<DatabaseSession>();
        services.AddScoped<IUnitOfWork, SharedUnitOfWork>();

        return services;
    }
}

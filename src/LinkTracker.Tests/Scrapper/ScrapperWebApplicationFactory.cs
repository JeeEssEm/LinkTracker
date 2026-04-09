using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;
using LinkTracker.Scrapper.Infrastructure.Migrations;

namespace LinkTracker.Tests.Scrapper;

public class ScrapperWebApplicationFactory : WebApplicationFactory<LinkTracker.Scrapper.Program>, IAsyncLifetime
{
    private PostgreSqlContainer? _dbContainer;

    public async Task InitializeAsync()
    {
        _dbContainer = new PostgreSqlBuilder("postgres:17").Build();
        await _dbContainer.StartAsync();
        DatabaseInitializer.Initialize(_dbContainer.GetConnectionString());
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        if (_dbContainer != null)
        {
            await _dbContainer.DisposeAsync();
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        var connectionString = _dbContainer!.GetConnectionString();
        builder.UseSetting("DatabaseOptions:ConnectionString", connectionString);

        builder.ConfigureServices(services =>
        {
            var quartzHostedServices = services
                .Where(d => d.ServiceType == typeof(IHostedService)
                            && d.ImplementationType?.Assembly.FullName?.Contains("Quartz") == true)
                .ToList();

            foreach (var descriptor in quartzHostedServices)
            {
                services.Remove(descriptor);
            }
        });
    }
}

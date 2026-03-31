using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LinkTracker.Tests.Scrapper;

public class ScrapperWebApplicationFactory : WebApplicationFactory<LinkTracker.Scrapper.Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

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

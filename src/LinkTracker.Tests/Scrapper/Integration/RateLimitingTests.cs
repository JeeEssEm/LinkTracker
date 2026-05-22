using System.Net;

namespace LinkTracker.Tests.Scrapper.Integration;

public class RateLimitingTests : IClassFixture<ScrapperWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RateLimitingTests(ScrapperWebApplicationFactory factory)
    {
        var configuredFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ResilienceOptions:RateLimitOptions:QueueLimit", "0");
            builder.UseSetting("ResilienceOptions:RateLimitOptions:WindowSeconds", "60");
            builder.UseSetting("ResilienceOptions:RateLimitOptions:PermitLimit", "3");
        });

        _client = configuredFactory.CreateClient();
    }

    [Fact]
    public async Task RateLimit_Returns429_WhenLimitExceeded()
    {
        for (var i = 0; i < 3; i++)
        {
            var response = await _client.PostAsync($"/tg-chat/{i}", null);
            Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
        }

        var exceeded = await _client.PostAsync("/tg-chat/4", null);
        Assert.Equal(HttpStatusCode.TooManyRequests, exceeded.StatusCode);
    }
}
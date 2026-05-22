using System.Net;
using System.Net.Http.Json;
using LinkTracker.Bot.Contracts.Rest;

namespace LinkTracker.Tests.Bot.Resilience;

public class RateLimitingTests : IClassFixture<BotWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RateLimitingTests(BotWebApplicationFactory factory)
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
            var response = await _client.PostAsync(
                "/updates",
                JsonContent.Create(new LinkUpdateRequest(Guid.NewGuid(), "www.example.com", "hi", [123123]))
            );
            Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
        }

        var exceeded = await _client.PostAsync(
            "/updates",
            JsonContent.Create(new LinkUpdateRequest(Guid.NewGuid(), "www.example.com", "hi", [123123])));
        Assert.Equal(HttpStatusCode.TooManyRequests, exceeded.StatusCode);
    }
}
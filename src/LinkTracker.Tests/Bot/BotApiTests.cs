using System.Net;
using System.Net.Http.Json;
using LinkTracker.Bot.Contracts.Rest;
using LinkTracker.Scrapper.ApiClients.BotNotifier;

namespace LinkTracker.Tests.Bot;

public class BotApiTests : IClassFixture<BotWebApplicationFactory>
{
    private readonly HttpClient _client;

    public BotApiTests(BotWebApplicationFactory botWebApplicationFactory)
    {
        _client = botWebApplicationFactory.CreateClient();
    }

    [Fact]
    public async Task UpdatesOk()
    {
        var resp = await _client.PostAsync(
            "/updates", JsonContent.Create(new LinkUpdateRequest(123, "www.example.com", "hi", [869822696])));
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task UpdatesFail_WrongSchema()
    {
        var resp = await _client.PostAsync(
            "/updates", JsonContent.Create(
                new { Id = 123, Url = "www.example.com" }
            ));
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }
}
using System.Net;
using System.Net.Http.Json;
using LinkTracker.Scrapper.Features.Links.AddLink;
using LinkTracker.Scrapper.Features.Links.GetLinks;
using LinkTracker.Scrapper.Features.Links.RemoveLink;

namespace LinkTracker.Tests.Scrapper;

public class ScrapperApiTests : IClassFixture<ScrapperWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ScrapperApiTests(ScrapperWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AddAndGetLink()
    {
        var response = await _client.PostAsync("/tg-chat/100", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var addLinkResponse = await PostLink(100, "https://github.com/dotnet/runtime");
        Assert.Equal(HttpStatusCode.OK, addLinkResponse.StatusCode);

        var getResponse = await GetLinks(100);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var body = await getResponse.Content.ReadFromJsonAsync<ListLinksResponse>();
        Assert.NotNull(body);
        Assert.True(body.Size > 0);
        Assert.Contains(body.Links, l => l.Url == "https://github.com/dotnet/runtime");
    }

    [Fact]
    public async Task AddAndRemoveLink()
    {
        await _client.PostAsync("/tg-chat/200", null);
        await PostLink(200, "https://github.com/dotnet/aspnetcore");

        var deleteResponse = await DeleteLink(200, "https://github.com/dotnet/aspnetcore");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        var getResponse = await GetLinks(200);
        var body = await getResponse.Content.ReadFromJsonAsync<ListLinksResponse>();
        Assert.NotNull(body);
        Assert.DoesNotContain(body.Links, l => l.Url == "https://github.com/dotnet/aspnetcore");
    }

    [Fact]
    public async Task RemoveLinkFromNonExistentChat()
    {
        await _client.PostAsync("/tg-chat/300", null);
        await PostLink(300, "https://github.com/dotnet/efcore");

        var deleteResponse = await DeleteLink(999, "https://github.com/dotnet/efcore");
        Assert.NotEqual(HttpStatusCode.OK, deleteResponse.StatusCode);

        var getResponse = await GetLinks(300);
        var body = await getResponse.Content.ReadFromJsonAsync<ListLinksResponse>();
        Assert.NotNull(body);
        Assert.Contains(body.Links, l => l.Url == "https://github.com/dotnet/efcore");
    }

    [Fact]
    public async Task AddLinkToNonExistentChat()
    {
        await _client.PostAsync("/tg-chat/400", null);

        var addResponse = await PostLink(401, "https://github.com/dotnet/runtime");
        Assert.NotEqual(HttpStatusCode.OK, addResponse.StatusCode);
    }

    [Fact]
    public async Task WorkWithRemovedChat()
    {
        await _client.PostAsync("/tg-chat/500", null);
        await _client.DeleteAsync("/tg-chat/500");

        var addResponse = await PostLink(500, "https://github.com/dotnet/runtime");
        Assert.NotEqual(HttpStatusCode.OK, addResponse.StatusCode);
    }

    [Fact]
    public async Task RemoveNonExistentChat()
    {
        var response = await _client.DeleteAsync("/tg-chat/600");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<HttpResponseMessage> PostLink(long chatId, string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/links");
        request.Headers.Add("Tg-Chat-Id", chatId.ToString());
        request.Content = JsonContent.Create(new AddLinkRequest(url, [], []));
        return await _client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> GetLinks(long chatId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/links");
        request.Headers.Add("Tg-Chat-Id", chatId.ToString());
        return await _client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> DeleteLink(long chatId, string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, "/links");
        request.Headers.Add("Tg-Chat-Id", chatId.ToString());
        request.Content = JsonContent.Create(new RemoveLinkRequest(url));
        return await _client.SendAsync(request);
    }
}

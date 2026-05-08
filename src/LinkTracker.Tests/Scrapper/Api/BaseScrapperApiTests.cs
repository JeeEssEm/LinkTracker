using System.Net;
using System.Net.Http.Json;
using LinkTracker.Scrapper.Contracts.Dtos.Links;
using ListLinksResponse = LinkTracker.Scrapper.Contracts.Dtos.Links.ListLinksResponse;

namespace LinkTracker.Tests.Scrapper.Api;

public abstract class BaseScrapperApiTests(ScrapperWebApplicationFactory factory, string accessType) : IClassFixture<ScrapperWebApplicationFactory>, IAsyncLifetime
{
    private HttpClient _client = null!;

    [Fact]
    public async Task AddAndGetLink()
    {
        var response = await _client.PostAsync("/tg-chat/100", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var addLinkResponse = await _client.PostLink(100, "https://github.com/dotnet/runtime", ["dotnet", "C#"]);
        Assert.Equal(HttpStatusCode.OK, addLinkResponse.StatusCode);

        var getResponse = await _client.GetLinks(100);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var body = await getResponse.Content.ReadFromJsonAsync<ListLinksResponse>();
        Assert.NotNull(body);
        Assert.True(body.Size == 1);
        Assert.Contains(body.Links, l => l.Url == "https://github.com/dotnet/runtime");

        var createdLink = body.Links.First();
        Assert.Equal(2, createdLink.Tags.Count);
        Assert.Contains("dotnet", createdLink.Tags);
        Assert.Contains("C#", createdLink.Tags);
    }

    [Fact]
    public async Task AddAndRemoveLink()
    {
        await _client.PostAsync("/tg-chat/200", null);
        await _client.PostLink(200, "https://github.com/dotnet/aspnetcore", ["Web", "Api", "C#"]);

        var deleteResponse = await _client.DeleteLink(200, "https://github.com/dotnet/aspnetcore");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
        var deletedLink = await deleteResponse.Content.ReadFromJsonAsync<LinkResponse>();
        Assert.NotNull(deletedLink);
        Assert.Equal(3, deletedLink.Tags.Count);
        Assert.Contains("Web", deletedLink.Tags);
        Assert.Contains("Api", deletedLink.Tags);
        Assert.Contains("C#", deletedLink.Tags);

        var getResponse = await _client.GetLinks(200);
        var body = await getResponse.Content.ReadFromJsonAsync<ListLinksResponse>();
        Assert.NotNull(body);
        Assert.DoesNotContain(body.Links, l => l.Url == "https://github.com/dotnet/aspnetcore");
    }

    [Fact]
    public async Task RemoveLinkFromNonExistentChat()
    {
        await _client.PostAsync("/tg-chat/300", null);
        await _client.PostLink(300, "https://github.com/dotnet/efcore", []);

        var deleteResponse = await _client.DeleteLink(999, "https://github.com/dotnet/efcore");
        Assert.NotEqual(HttpStatusCode.OK, deleteResponse.StatusCode);

        var getResponse = await _client.GetLinks(300);
        var body = await getResponse.Content.ReadFromJsonAsync<ListLinksResponse>();
        Assert.NotNull(body);
        Assert.Contains(body.Links, l => l.Url == "https://github.com/dotnet/efcore");
    }

    [Fact]
    public async Task AddLinkToNonExistentChat()
    {
        await _client.PostAsync("/tg-chat/400", null);

        var addResponse = await _client.PostLink(401, "https://github.com/dotnet/runtime", []);
        Assert.NotEqual(HttpStatusCode.OK, addResponse.StatusCode);
    }

    [Fact]
    public async Task WorkWithRemovedChat()
    {
        await _client.PostAsync("/tg-chat/500", null);
        await _client.DeleteAsync("/tg-chat/500");

        var addResponse = await _client.PostLink(500, "https://github.com/dotnet/runtime", []);
        Assert.NotEqual(HttpStatusCode.OK, addResponse.StatusCode);
    }

    [Fact]
    public async Task RemoveNonExistentChat()
    {
        var response = await _client.DeleteAsync("/tg-chat/600");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task TwoChatsOneLink()
    {
        await _client.PostAsync("/tg-chat/528", null);
        await _client.PostAsync("/tg-chat/488", null);

        await _client.PostLink(528, "https://stackoverflow.com/questions/123", ["java", "web"]);
        await _client.PostLink(488, "https://stackoverflow.com/questions/123", ["spring", "api", "web"]);

        await _client.DeleteLink(488, "https://stackoverflow.com/questions/123");

        var resp528 = await _client.GetLinks(528);
        var resp488 = await _client.GetLinks(488);

        var links528 = await resp528.Content.ReadFromJsonAsync<ListLinksResponse>();
        var links488 = await resp488.Content.ReadFromJsonAsync<ListLinksResponse>();
        Assert.NotNull(links528);
        Assert.NotNull(links488);

        Assert.Equal(1, links528.Size);
        Assert.Equal(0, links488.Size);

        var first = links528.Links.First();
        Assert.Contains("java", first.Tags);
        Assert.Contains("web", first.Tags);
    }



    public Task InitializeAsync()
    {
        var testHost = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("DatabaseOptions:AccessType", accessType);
        });
        _client = testHost.CreateClient();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}

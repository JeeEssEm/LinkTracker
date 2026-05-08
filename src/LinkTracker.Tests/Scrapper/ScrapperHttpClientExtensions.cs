using System.Net.Http.Json;
using LinkTracker.Scrapper.Contracts.Dtos.Links;

namespace LinkTracker.Tests.Scrapper;

public static class ScrapperHttpClientExtensions
{
    public static async Task<HttpResponseMessage> PostLink(this HttpClient client, long chatId, string url, List<string> tags)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/links");
        request.Headers.Add("Tg-Chat-Id", chatId.ToString());
        request.Content = JsonContent.Create(new AddLinkRequest(url, tags, []));
        return await client.SendAsync(request);
    }

    public static async Task<HttpResponseMessage> GetLinks(this HttpClient client, long chatId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/links");
        request.Headers.Add("Tg-Chat-Id", chatId.ToString());
        return await client.SendAsync(request);
    }

    public static async Task<HttpResponseMessage> DeleteLink(this HttpClient client, long chatId, string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, "/links");
        request.Headers.Add("Tg-Chat-Id", chatId.ToString());
        request.Content = JsonContent.Create(new RemoveLinkRequest(url));
        return await client.SendAsync(request);
    }
}
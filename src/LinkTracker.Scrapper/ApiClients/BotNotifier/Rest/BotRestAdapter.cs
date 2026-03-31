using FluentResults;

namespace LinkTracker.Scrapper.ApiClients.BotNotifier.Rest;

public class BotRestAdapter(IBotRefitClient botRefitClient) : IBotClient
{
    public async Task<Result> SendUpdateAsync(LinkUpdateRequest request)
    {
        var response = await botRefitClient.SendUpdateAsync(request);
        if (response.IsSuccessStatusCode)
        {
            return Result.Ok();
        }

        return Result.Fail(new Error(response.RequestMessage?.ToString()));
    }
}
using LinkTracker.Scrapper.ApiClients;
using LinkTracker.Scrapper.ApiClients.BotNotifier;
using LinkTracker.Scrapper.Domain;
using LinkTracker.Scrapper.Features.Shared.Abstractions;
using Quartz;

namespace LinkTracker.Scrapper.Scheduling;

public class LinkCheckJob(
    ILinkStore linkStore,
    LinkCheckerResolver checkerResolver,
    IBotClient botClient,
    ILogger<LinkCheckJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var links = await linkStore.GetAllLinksAsync();

        foreach (var link in links)
        {
            await ProcessLink(link);
        }
    }

    private async Task ProcessLink(Link link)
    {
        if (!Uri.TryCreate(link.Url, UriKind.Absolute, out var uri))
        {
            return;
        }

        var checkerResult = checkerResolver.GetByKey(link.CheckerKey);
        if (checkerResult.IsFailed)
        {
            return;
        }

        try
        {
            var result = await checkerResult.Value.CheckAsync(uri, link.LastCheckedAt);

            if (result.HasUpdate)
            {
                await HandleUpdate(link, result.Message);
            }

            await linkStore.UpdateLastCheckedAt(link.Url, link.Owner.Id, DateTimeOffset.UtcNow);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking link {Url}", link.Url);
        }
    }

    private async Task HandleUpdate(Link link, string message)
    {
        logger.LogInformation("Update found for {Url}: {Message}", link.Url, message);

        var chatIds = await GetChatIdsToNotify(link);

        var update = new LinkUpdateRequest(
            link.Id,
            link.Url,
            message,
            chatIds);

        var result = await botClient.SendUpdateAsync(update);
        // подумать, как ретраить, если:
        // 1. юзер вообще забанен в телеграме
        // 2. сервис пока что лежит/телеграм лежит suddenly
        if (result.IsFailed)
        {
            logger.LogError("Failed to notify bot about update for {Url}", link.Url);
        }
    }

    private async Task<List<long>> GetChatIdsToNotify(Link link)
    {
        // TODO: переписать на батчинг.
        var allLinks = await linkStore.GetAllLinksAsync();
        var chatIds = allLinks
            .Where(l => l.Url == link.Url)
            .Select(l => l.Owner.Id)
            .Distinct()
            .ToList();
        return chatIds;
    }
}

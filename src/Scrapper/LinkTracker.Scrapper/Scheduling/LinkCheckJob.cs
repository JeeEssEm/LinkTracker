using FluentResults;
using LinkTracker.Bot.Contracts.Rest;
using LinkTracker.Scrapper.ApiClients;
using LinkTracker.Scrapper.ApiClients.BotNotifier;
using LinkTracker.Scrapper.ApiClients.Shared;
using LinkTracker.Scrapper.Application.Repositories;
using LinkTracker.Scrapper.Application.Scheduling;
using LinkTracker.Scrapper.Configurations;
using Microsoft.Extensions.Options;
using Quartz;

namespace LinkTracker.Scrapper.Scheduling;

public class LinkCheckJob(
    ILinkRepository linkRepository,
    IServiceScopeFactory scopeFactory,
    IOptions<SchedulerOptions> schedulerOptions,
    ILogger<LinkCheckJob> logger
    ) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var page = 1;
        var options = new ParallelOptions() { MaxDegreeOfParallelism = schedulerOptions.Value.MaxDegreeOfParallelism, CancellationToken = context.CancellationToken };

        List<LinkCheckEntry> links;
        var checkTime = DateTime.UtcNow;

        while ((links = await linkRepository.GetLinksToCheck(page, schedulerOptions.Value.BatchSize, checkTime)).Any())
        {
            await Parallel.ForEachAsync(links, options, async (link, _) =>
            {
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var repo = scope.ServiceProvider.GetRequiredService<ILinkRepository>();
                    var checkerResolver = scope.ServiceProvider.GetRequiredService<LinkCheckerResolver>();
                    var botClient = scope.ServiceProvider.GetRequiredService<IBotClient>();

                    DateTime? changeTime = null;
                    var result = await ProcessLink(link, checkerResolver);

                    if (result.IsSuccess && result.Value.HasUpdate)
                    {
                        try
                        {
                            await NotifyUsers(botClient, new NotifyEntry(link.Id, link.Url, result.Value.Message, link.ChatIdsToNotify));
                        }
                        catch (Exception e)
                        {
                            logger.LogError(e, "Failed to notify users");
                        }

                        changeTime = checkTime;
                    }

                    // TODO: а если ссылка удалится во время проверки? В лучшем случае LinkNotFound. В худшем - хз, исключение
                    await repo.UpdateCheckDatesAt(link.Id, checkTime, changeTime);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Failed to process link");
                }
            });
            page++;
        }
    }

    private static async Task NotifyUsers(IBotClient botClient, NotifyEntry entry)
    {
        var request = new LinkUpdateRequest(entry.LinkId, entry.Url, entry.Message, entry.ChatIds);
        await botClient.SendUpdateAsync(request);
    }

    private async Task<Result<UpdateCheckResult>> ProcessLink(LinkCheckEntry link, LinkCheckerResolver checkerResolver)
    {
        if (!Uri.TryCreate(link.Url, UriKind.Absolute, out var uri))
        {
            return Result.Fail("Cannot create URI!");
        }

        var checkerResult = checkerResolver.GetByKey(link.CheckerKey);
        if (checkerResult.IsFailed)
        {
            return checkerResult.ToResult();
        }

        return await checkerResult.Value.CheckAsync(uri, link.LastChangeAt);
    }
}

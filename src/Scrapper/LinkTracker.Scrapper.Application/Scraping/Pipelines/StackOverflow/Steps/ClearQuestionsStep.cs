using System.Runtime.CompilerServices;
using System.Threading.Tasks.Dataflow;
using LinkTracker.Scrapper.Application.Abstractions;
using LinkTracker.Scrapper.Application.Repositories;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Notifications;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Data;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Results;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Store;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Steps;

public class ClearQuestionsStep(
    IServiceProvider serviceProvider,
    ILogger<ClearQuestionsStep> logger)
{
    public async Task<IReadOnlyList<QuestionData>> ProcessAsync(
        FetchQuestionsResult result,
        ITargetBlock<NotificationMessage> aggregator)
    {
        if (result.BannedQuestions.Count == 0)
        {
            return result.FoundQuestions;
        }

        try
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var store = scope.ServiceProvider.GetRequiredService<IStackOverflowStore>();
            var linkRepo = scope.ServiceProvider.GetRequiredService<ILinkRepository>();
            var subRepo = scope.ServiceProvider.GetRequiredService<ISubscriptionRepository>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            await uow.BeginAsync();

            await store.RemoveQuestionsByIds(result.BannedQuestions.Select(q => q.Id));
            await linkRepo.RemoveLinksAsync(result.BannedQuestions.Select(q => q.Link.Id));

            await uow.CommitAsync();

            await NotifyBannedAsync(result.BannedQuestions, subRepo, aggregator);

            logger.LogInformation("ClearQuestions: removed {Count} banned questions", result.BannedQuestions.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to clear banned questions from DB");
        }

        return result.FoundQuestions;
    }

    private static async Task NotifyBannedAsync(
        IReadOnlyList<QuestionData> bannedQuestions,
        ISubscriptionRepository subRepo,
        ITargetBlock<NotificationMessage> aggregator)
    {
        foreach (var q in bannedQuestions)
        {
            await aggregator.SendAsync(new NotificationMessage(
                q.Link.ChatIdsToNotify,
                $"Ваша ссылка больше не отслеживается, так как она не существует на StackOverflow",
                q.Link.Url,
                q.Link.Id));
        }
    }
}
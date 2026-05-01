using LinkTracker.Bot.Contracts.Rest;
using LinkTracker.Scrapper.Application.Abstractions.UnitOfWork;
using LinkTracker.Scrapper.Application.Dtos.Outbox;
using LinkTracker.Scrapper.Application.Repositories;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Data;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Steps;

public class ClearQuestionsStep(
    IServiceProvider serviceProvider,
    ILogger<ClearQuestionsStep> logger)
{
    public async Task<IReadOnlyList<QuestionData>> ProcessAsync(
        FetchQuestionsResult result)
    {
        if (result.BannedQuestions.Count == 0)
        {
            return result.FoundQuestions;
        }

        try
        {
            // TODO: сделать сами шаги scoped, чтоб не париться с зависимостями
            await using var scope = serviceProvider.CreateAsyncScope();
            var linkRepo = scope.ServiceProvider.GetRequiredService<ILinkBulkRepository>();
            var outboxRepo = scope.ServiceProvider.GetRequiredService<IOutboxEventRepository>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            await uow.BeginAsync();

            await linkRepo.RemoveLinksAsync(result.BannedQuestions.Select(q => q.Link.Id));

            await NotifyBannedAsync(result.BannedQuestions, outboxRepo);

            await uow.CommitAsync();

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
        IOutboxEventRepository outboxRepo)
    {
        var events = bannedQuestions.Select(q => new CreateOutboxEvent(
            new LinkUpdateRequest(q.Link.Id, q.Link.Url, "Ваша ссылка больше не отслеживается, так как она не существует на StackOverflow", q.Link.ChatIdsToNotify)
        ));
        await outboxRepo.CreateAsync(events);
    }
}
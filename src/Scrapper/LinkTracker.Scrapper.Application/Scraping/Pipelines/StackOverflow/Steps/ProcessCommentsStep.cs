using LinkTracker.Scrapper.Application.Repositories;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Results;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Store;
using LinkTracker.Bot.Contracts.Rest;
using LinkTracker.Scrapper.Application.Abstractions.UnitOfWork;
using LinkTracker.Scrapper.Application.Dtos.Outbox;
using LinkTracker.Scrapper.Contracts.Dtos.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Steps;

public class ProcessCommentsStep(
    IServiceProvider serviceProvider,
    ILogger<ProcessCommentsStep> logger)
{
    private const int PreviewLength = 200;

    public async Task ProcessAsync(
        FetchCommentsResult result)
    {
        try
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var store = scope.ServiceProvider.GetRequiredService<IStackOverflowStore>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var outboxRepo = scope.ServiceProvider.GetRequiredService<IOutboxEventRepository>();

            await uow.BeginAsync();

            await SyncDeletedAsync(store, result);

            if (result.FoundComments.Count == 0)
            {
                return;
            }

            var newComments = await store.CreateAndReturnIfNotExist(result.FoundComments.ToArray());

            logger.LogInformation(
                "ProcessComments: total={Total}, new={New}, missingAnswers={Missing}",
                result.FoundComments.Count, newComments.Length, result.MissingAnswerIds.Count);
            var events = new List<CreateOutboxEvent>();

            foreach (var comment in newComments)
            {
                if (!comment.Answer.Question.Link.IsFirstCheck)
                {
                    events.Add(new CreateOutboxEvent(new UpdateMessage(
                        comment.Answer.Question.Link.Id,
                        comment.Answer.Question.Link.Url,
                        comment.OwnerDisplayName,
                        $"""
                         📌 {comment.Answer.Question.Link.Url}

                         💬 Новый комментарий от {comment.OwnerDisplayName} ({comment.CreatedAt:dd.MM.yyyy HH:mm} UTC)

                         {Truncate(comment.Body, PreviewLength)}
                         """,
                        comment.Answer.Question.Link.ChatIdsToNotify
                    )));
                }
            }
            await outboxRepo.CreateAsync(events);
            await uow.CommitAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process comments batch");
        }
    }

    private static async Task SyncDeletedAsync(IStackOverflowStore store, FetchCommentsResult result)
    {
        if (result.FoundComments.Count > 0)
        {
            var currentCommentIds = result.FoundComments.Select(c => c.Id);
            var affectedAnswerIds = result.FoundComments.Select(c => c.Answer.Id).Distinct();
            await store.RemoveStaleComments(affectedAnswerIds, currentCommentIds);
        }

        if (result.MissingAnswerIds.Count > 0)
        {
            await store.RemoveCommentsByAnswerIds(result.MissingAnswerIds);
        }
    }

    private static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..maxLength] + "…";
}
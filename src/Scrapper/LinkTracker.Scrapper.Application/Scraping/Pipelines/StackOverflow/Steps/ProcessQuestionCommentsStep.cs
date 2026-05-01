using LinkTracker.Scrapper.Application.Repositories;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Results;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Store;
using LinkTracker.Bot.Contracts.Rest;
using LinkTracker.Scrapper.Application.Abstractions.UnitOfWork;
using LinkTracker.Scrapper.Application.Dtos.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Steps;

public class ProcessQuestionCommentsStep(
    IServiceProvider serviceProvider,
    ILogger<ProcessQuestionCommentsStep> logger)
{
    private const int PreviewLength = 200;

    public async Task ProcessAsync(FetchQuestionCommentsResult result)
    {
        try
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var store = scope.ServiceProvider.GetRequiredService<IStackOverflowStore>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var outboxRepo = scope.ServiceProvider.GetRequiredService<IOutboxEventRepository>();

            await uow.BeginAsync();

            await SyncDeletedAsync(store, result);

            if (result.ReceivedComments.Count == 0)
            {
                return;
            }

            var newComments = await store.CreateAndReturnIfNotExist(result.ReceivedComments.ToArray());

            logger.LogInformation(
                "ProcessQuestionComments: total={Total}, new={New}, missingQuestions={Missing}",
                result.ReceivedComments.Count, newComments.Length, result.MissingQuestionIds.Count);
            var events = new List<CreateOutboxEvent>();
            foreach (var comment in newComments)
            {
                if (!comment.Question.Link.IsFirstCheck)
                {
                    events.Add(new CreateOutboxEvent(new LinkUpdateRequest(
                        comment.Question.Link.Id,
                        comment.Question.Link.Url,
                        $"""
                         📌 {comment.Question.Link.Url} 

                         💬 Новый комментарий от {comment.OwnerDisplayName} ({comment.CreatedAt:dd.MM.yyyy HH:mm} UTC)
                         {comment.Question.Title}
                         {Truncate(comment.Body, PreviewLength)}
                         """,
                        comment.Question.Link.ChatIdsToNotify
                        )
                    ));
                }
            }

            await outboxRepo.CreateAsync(events);
            await uow.CommitAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process question comments batch");
        }
    }

    private static async Task SyncDeletedAsync(IStackOverflowStore store, FetchQuestionCommentsResult result)
    {
        if (result.ReceivedComments.Count > 0)
        {
            var currentCommentIds = result.ReceivedComments.Select(c => c.Id);
            var affectedQuestionIds = result.ReceivedComments.Select(c => c.Question.Id).Distinct();
            await store.RemoveStaleQuestionComments(affectedQuestionIds, currentCommentIds);
        }

        if (result.MissingQuestionIds.Count > 0)
        {
            await store.RemoveQuestionCommentsByQuestionIds(result.MissingQuestionIds);
        }
    }

    private static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..maxLength] + "…";
}

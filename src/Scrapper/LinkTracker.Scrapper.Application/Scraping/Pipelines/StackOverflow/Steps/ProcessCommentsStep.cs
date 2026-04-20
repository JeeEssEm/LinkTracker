using LinkTracker.Scrapper.Application.Repositories;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Notifications;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Data;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Results;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Store;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Steps;

public class ProcessCommentsStep(
    IServiceProvider serviceProvider,
    ILogger<ProcessCommentsStep> logger)
{
    private const int PreviewLength = 200;

    public async Task ProcessAsync(
        FetchCommentsResult result,
        ITargetBlock<NotificationMessage> aggregator)
    {
        try
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var store = scope.ServiceProvider.GetRequiredService<IStackOverflowStore>();
            var subRepo = scope.ServiceProvider.GetRequiredService<ISubscriptionRepository>();

            await SyncDeletedAsync(store, result);

            if (result.FoundComments.Count == 0)
            {
                return;
            }

            var newComments = await store.CreateAndReturnIfNotExist(result.FoundComments.ToArray());

            logger.LogInformation(
                "ProcessComments: total={Total}, new={New}, missingAnswers={Missing}",
                result.FoundComments.Count, newComments.Length, result.MissingAnswerIds.Count);

            foreach (var comment in newComments)
            {
                if (!comment.Answer.Question.Link.IsFirstCheck)
                {
                    await NotifyAsync(comment, subRepo, aggregator);
                }
            }
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

    private static async Task NotifyAsync(
        CommentData comment,
        ISubscriptionRepository subRepo,
        ITargetBlock<NotificationMessage> aggregator)
    {
        var link = comment.Answer.Question.Link;
        if (link.ChatIdsToNotify.Count == 0)
        {
            return;
        }

        var text =
            $"""
             📌 {comment.Answer.Question.Link.Id}

             💬 Новый комментарий от {comment.OwnerDisplayName} ({comment.CreatedAt:dd.MM.yyyy HH:mm} UTC)

             {Truncate(comment.Body, PreviewLength)}
             """;
        await aggregator.SendAsync(new NotificationMessage(
            link.ChatIdsToNotify, text, link.Url, link.Id));
    }

    private static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..maxLength] + "…";
}
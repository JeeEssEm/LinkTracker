using LinkTracker.Scrapper.Application.Repositories;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Notifications;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Data;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Results;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Store;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Steps;

public class ProcessQuestionCommentsStep(
    IServiceProvider serviceProvider,
    ILogger<ProcessQuestionCommentsStep> logger)
{
    private const int PreviewLength = 200;

    public async Task ProcessAsync(
        FetchQuestionCommentsResult result,
        ITargetBlock<NotificationMessage> aggregator)
    {
        try
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var store = scope.ServiceProvider.GetRequiredService<IStackOverflowStore>();
            var subRepo = scope.ServiceProvider.GetRequiredService<ISubscriptionRepository>();

            await SyncDeletedAsync(store, result);

            if (result.ReceivedComments.Count == 0)
            {
                return;
            }

            var newComments = await store.CreateAndReturnIfNotExist(result.ReceivedComments.ToArray());

            logger.LogInformation(
                "ProcessQuestionComments: total={Total}, new={New}, missingQuestions={Missing}",
                result.ReceivedComments.Count, newComments.Length, result.MissingQuestionIds.Count);

            foreach (var comment in newComments)
            {
                if (!comment.Question.Link.IsFirstCheck)
                {
                    await NotifyAsync(comment, subRepo, aggregator);
                }
            }
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

    private static async Task NotifyAsync(
        QuestionCommentData comment,
        ISubscriptionRepository subRepo,
        ITargetBlock<NotificationMessage> aggregator)
    {
        var link = comment.Question.Link;
        if (link.ChatIdsToNotify.Count == 0)
        {
            return;
        }

        var text =
            $"""
             📌 {comment.Question.Title}

             💬 Новый комментарий от {comment.OwnerDisplayName} ({comment.CreatedAt:dd.MM.yyyy HH:mm} UTC)

             {Truncate(comment.Body, PreviewLength)}
             """;

        await aggregator.SendAsync(new NotificationMessage(link.ChatIdsToNotify, text, link.Url, link.Id));
    }

    private static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..maxLength] + "…";
}

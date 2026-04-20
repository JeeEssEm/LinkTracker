using System.Threading.Tasks.Dataflow;
using LinkTracker.Scrapper.Application.Repositories;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Notifications;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Shared;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Data;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Store;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Steps;

public class ProcessAnswersStep(
    IServiceProvider serviceProvider,
    ILogger<ProcessAnswersStep> logger)
{
    private const int PreviewLength = 200;

    public async Task ProcessAsync(
        IReadOnlyList<AnswerData> batch,
        ITargetBlock<NotificationMessage> aggregator)
    {
        if (batch.Count == 0)
        {
            return;
        }

        try
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var store = scope.ServiceProvider.GetRequiredService<IStackOverflowStore>();

            var newAnswers = await store.CreateAndReturnIfNotExist(batch.ToArray());

            logger.LogInformation(
                "ProcessAnswers: total={Total}, new={New}",
                batch.Count, newAnswers.Length);

            foreach (var answer in newAnswers)
            {
                if (!answer.Question.Link.IsFirstCheck)
                {
                    await NotifyAsync(answer, aggregator);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process answers batch");
        }
    }

    private static async Task NotifyAsync(
        AnswerData answer,
        ITargetBlock<NotificationMessage> aggregator)
    {
        var link = answer.Question.Link;
        if (link.ChatIdsToNotify.Count == 0)
        {
            return;
        }

        var text =
            $"""
             📌 {answer.Question.Title}

             💬 Новый ответ от {answer.OwnerDisplayName} ({answer.CreatedAt:dd.MM.yyyy HH:mm} UTC)

             {TextUtilities.Truncate(answer.Body, PreviewLength)}
             """;

        await aggregator.SendAsync(new NotificationMessage(link.ChatIdsToNotify, text, answer.Question.Link.Url, answer.Question.Link.Id));
    }
}
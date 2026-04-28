using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Data;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Results;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Store;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Steps;

public class ClearAnswersStep(
    IServiceProvider serviceProvider,
    ILogger<ClearAnswersStep> logger)
{
    public async Task<IReadOnlyList<AnswerData>> ProcessAsync(FetchAnswersResult batch)
    {
        try
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var store = scope.ServiceProvider.GetRequiredService<IStackOverflowStore>();

            // Удаляем ответы, которые были в БД, но API их больше не отдаёт
            if (batch.FoundAnswers.Count > 0)
            {
                var currentAnswerIds = batch.FoundAnswers.Select(a => a.Id);
                var affectedQuestionIds = batch.FoundAnswers.Select(a => a.Question.Id).Distinct();
                await store.RemoveStaleAnswers(affectedQuestionIds, currentAnswerIds);
            }

            // Удаляем все ответы для вопросов, у которых теперь 0 ответов
            if (batch.MissingQuestionIds.Count > 0)
            {
                await store.RemoveAnswersByQuestionIds(batch.MissingQuestionIds);

                logger.LogInformation(
                    "ClearAnswers: cleared all answers for {Count} questions with no API results",
                    batch.MissingQuestionIds.Count);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to clear stale answers");
        }

        return batch.FoundAnswers;
    }
}
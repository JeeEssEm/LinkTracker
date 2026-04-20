using LinkTracker.Scrapper.Application.Scraping.ApiClients.StackOverflow;
using LinkTracker.Scrapper.Application.Scraping.ApiClients.StackOverflow.Models;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Data;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Steps;

public class FetchQuestionsStep(
    IServiceProvider serviceProvider,
    ILogger<FetchQuestionsStep> logger,
    IOptions<StackOverflowPipelineOptions> options)
{
    private readonly StackOverflowPipelineOptions _options = options.Value;

    public async Task<FetchQuestionsResult> ProcessAsync(List<QuestionData> batch)
    {
        var fetched = new List<QuestionResponse>();

        try
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var client = scope.ServiceProvider.GetRequiredService<IStackOverflowClient>();

            foreach (var chunk in batch.Select(q => q.Id).ToArray().Chunk(_options.StackOverflowLimit))
            {
                try
                {
                    fetched.AddRange(await FetchChunkAsync(client, chunk));
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Failed to fetch questions chunk");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch questions");
            return new FetchQuestionsResult([], []);
        }

        var fetchedIds = fetched.Select(q => q.QuestionId).ToHashSet();
        var found = batch.Where(q => fetchedIds.Contains(q.Id)).ToList();
        var banned = batch.Where(q => !fetchedIds.Contains(q.Id)).ToList();

        logger.LogInformation("FetchQuestions: found={Found}, banned={Banned}", found.Count, banned.Count);

        return new FetchQuestionsResult(found, banned);
    }

    private async Task<List<QuestionResponse>> FetchChunkAsync(IStackOverflowClient client, long[] chunk)
    {
        var questions = new List<QuestionResponse>();
        var page = 1;

        while (true)
        {
            var result = await client.GetQuestions(chunk, page, _options.ApiPageSize);

            if (!result.IsSuccess)
            {
                logger.LogWarning("SO API returned error for questions: {Errors}", result.Errors);
                break;
            }

            questions.AddRange(result.Value.Questions);

            if (!result.Value.HasMore)
            {
                break;
            }

            page++;
        }

        return questions;
    }
}
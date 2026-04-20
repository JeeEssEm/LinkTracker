using LinkTracker.Scrapper.Application.Scraping.ApiClients.StackOverflow;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Data;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Steps;

public class FetchAnswersStep(
    IServiceProvider serviceProvider,
    ILogger<FetchAnswersStep> logger,
    IOptions<StackOverflowPipelineOptions> options)
{
    private readonly StackOverflowPipelineOptions _options = options.Value;

    public async Task<IEnumerable<FetchAnswersResult>> ProcessAsync(IReadOnlyList<QuestionData> batch)
    {
        var answers = new List<AnswerData>();

        try
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var client = scope.ServiceProvider.GetRequiredService<IStackOverflowClient>();

            var questionToLink = batch.ToDictionary(d => d.Id);

            foreach (var chunk in batch.Select(q => q.Id).ToArray().Chunk(_options.StackOverflowLimit))
            {
                try
                {
                    answers.AddRange(await FetchChunkAsync(client, chunk, questionToLink));
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Failed to fetch answers chunk");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch answers");
            return [];
        }

        var questionIds = batch.Select(q => q.Id).ToArray();
        var missingIds = FindMissingQuestionIds(questionIds, answers);

        logger.LogInformation(
            "FetchAnswers: answers={Answers}, missingQuestions={Missing}",
            answers.Count, missingIds.Count);

        return BuildBatches(answers, missingIds, _options.AnswersBatchSize);
    }

    private async Task<List<AnswerData>> FetchChunkAsync(
        IStackOverflowClient client,
        long[] chunk,
        Dictionary<long, QuestionData> questionToLink)
    {
        var answers = new List<AnswerData>();
        var page = 1;

        while (true)
        {
            var result = await client.GetAnswers(chunk, page, _options.ApiPageSize);

            if (!result.IsSuccess)
            {
                logger.LogWarning("SO API returned error for answers: {Errors}", result.Errors);
                break;
            }

            answers.AddRange(result.Value.Answers
                .Select(a => new AnswerData(
                    a.AnswerId,
                    questionToLink[a.QuestionId],
                    a.Body,
                    a.Username,
                    a.CreatedAt
                )));

            if (!result.Value.HasMore)
            {
                break;
            }

            page++;
        }

        return answers;
    }

    private static List<long> FindMissingQuestionIds(long[] requested, List<AnswerData> received)
    {
        var receivedIds = received.Select(a => a.Question.Id).ToHashSet();
        return requested.Where(id => !receivedIds.Contains(id)).ToList();
    }

    /// <summary>
    /// Нарезает answers на батчи, не разрывая группы одного вопроса.
    /// MissingQuestionIds равномерно распределяются по батчам.
    /// </summary>
    private static List<FetchAnswersResult> BuildBatches(
        List<AnswerData> answers,
        List<long> missingIds,
        int batchSize)
    {
        var answerBatches = new List<List<AnswerData>>();
        var currentBatch = new List<AnswerData>();

        foreach (var group in answers.GroupBy(a => a.Question.Id))
        {
            // Если добавление этой группы переполнит батч, то сохраняем текущий
            if (currentBatch.Count > 0 && currentBatch.Count + group.Count() > batchSize)
            {
                answerBatches.Add(currentBatch);
                currentBatch = [];
            }

            currentBatch.AddRange(group);
        }

        if (currentBatch.Count > 0)
        {
            answerBatches.Add(currentBatch);
        }

        // Если пришли только missingIds без ответов — нужен хотя бы один батч
        if (answerBatches.Count == 0 && missingIds.Count > 0)
        {
            answerBatches.Add([]);
        }

        var result = new List<FetchAnswersResult>();
        var maxBatches = Math.Max(answerBatches.Count, 1);

        // Равномерно раскладываем missingIds по батчам
        var missingChunks = missingIds.Count > 0
            ? missingIds
                .Chunk((int)Math.Ceiling((double)missingIds.Count / maxBatches))
                .Select(c => c.ToList())
                .ToList()
            : new List<List<long>>();

        for (var i = 0; i < maxBatches; i++)
        {
            var aChunk = i < answerBatches.Count ? answerBatches[i] : [];
            var mChunk = i < missingChunks.Count ? missingChunks[i] : [];
            result.Add(new FetchAnswersResult(aChunk, mChunk));
        }

        return result;
    }
}
using LinkTracker.Scrapper.Application.Scraping.ApiClients.StackOverflow;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Data;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Steps;

public class FetchCommentsStep(
    IServiceProvider serviceProvider,
    ILogger<FetchCommentsStep> logger,
    IOptions<StackOverflowPipelineOptions> options)
{
    private readonly StackOverflowPipelineOptions _options = options.Value;

    public async Task<FetchCommentsResult> ProcessAsync(IReadOnlyList<AnswerData> batch)
    {
        var comments = new List<CommentData>();

        var answerToContext = batch.ToDictionary(a => a.Id);

        try
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var client = scope.ServiceProvider.GetRequiredService<IStackOverflowClient>();

            foreach (var chunk in batch.Select(a => a.Id).ToArray().Chunk(_options.StackOverflowLimit))
            {
                try
                {
                    comments.AddRange(await FetchChunkAsync(client, chunk, answerToContext));
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Failed to fetch comments chunk");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch comments");
            return new FetchCommentsResult([], []);
        }

        var requestedIds = batch.Select(a => a.Id).ToArray();
        var missingIds = FindMissingAnswerIds(requestedIds, comments);

        logger.LogInformation(
            "FetchComments: comments={Comments}, missingAnswers={Missing}",
            comments.Count, missingIds.Count);

        return new FetchCommentsResult(comments, missingIds);
    }

    private async Task<List<CommentData>> FetchChunkAsync(
        IStackOverflowClient client,
        long[] answerIds,
        Dictionary<long, AnswerData> answerToContext)
    {
        var comments = new List<CommentData>();
        var page = 1;

        while (true)
        {
            var result = await client.GetComments(answerIds, page, _options.ApiPageSize);

            if (!result.IsSuccess)
            {
                logger.LogWarning("SO API returned error for comments: {Errors}", result.Errors);
                break;
            }

            foreach (var c in result.Value.Comments)
            {
                if (!answerToContext.TryGetValue(c.PostId, out var ctx))
                {
                    continue;
                }

                comments.Add(new CommentData(
                    c.CommentId,
                    ctx,
                    c.Body,
                    c.Username,
                    c.CreationDate
                    )
                );
            }

            if (!result.Value.HasMore)
            {
                break;
            }

            page++;
        }

        return comments;
    }

    private static List<long> FindMissingAnswerIds(long[] requested, List<CommentData> received)
    {
        var receivedIds = received.Select(c => c.Answer.Id).ToHashSet();
        return requested.Where(id => !receivedIds.Contains(id)).ToList();
    }
}
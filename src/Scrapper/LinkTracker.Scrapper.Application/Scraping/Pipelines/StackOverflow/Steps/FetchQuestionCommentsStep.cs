using LinkTracker.Scrapper.Application.Scraping.ApiClients.StackOverflow;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Data;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Steps;

public class FetchQuestionCommentsStep(
    IServiceProvider serviceProvider,
    ILogger<FetchQuestionCommentsStep> logger,
    IOptions<StackOverflowPipelineOptions> options)
{
    private readonly StackOverflowPipelineOptions _options = options.Value;

    public async Task<FetchQuestionCommentsResult> ProcessAsync(IReadOnlyList<QuestionData> batch)
    {
        var comments = new List<QuestionCommentData>();

        var questionToContext = batch.ToDictionary(q => q.Id);

        try
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var client = scope.ServiceProvider.GetRequiredService<IStackOverflowClient>();

            foreach (var chunk in batch.Select(q => q.Id).ToArray().Chunk(_options.StackOverflowLimit))
            {
                try
                {
                    comments.AddRange(await FetchChunkAsync(client, chunk, questionToContext));
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Failed to fetch question comments chunk");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch question comments");
            return new FetchQuestionCommentsResult([], []);
        }

        var requestedIds = batch.Select(q => q.Id).ToArray();
        var missingIds = FindMissingQuestionIds(requestedIds, comments);

        logger.LogInformation(
            "FetchQuestionComments: comments={Comments}, missingQuestions={Missing}",
            comments.Count, missingIds.Count);

        return new FetchQuestionCommentsResult(comments, missingIds);
    }

    private async Task<List<QuestionCommentData>> FetchChunkAsync(
        IStackOverflowClient client,
        long[] questionIds,
        Dictionary<long, QuestionData> questionToContext)
    {
        var comments = new List<QuestionCommentData>();
        var page = 1;

        while (true)
        {
            var result = await client.GetQuestionComments(questionIds, page, _options.ApiPageSize);

            if (!result.IsSuccess)
            {
                logger.LogWarning("SO API returned error for question comments: {Errors}", result.Errors);
                break;
            }

            foreach (var c in result.Value.Comments)
            {
                if (!questionToContext.TryGetValue(c.PostId, out var ctx))
                {
                    continue;
                }

                comments.Add(new QuestionCommentData(
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

    private static List<long> FindMissingQuestionIds(long[] requested, List<QuestionCommentData> received)
    {
        var receivedIds = received.Select(c => c.Question.Id).ToHashSet();
        return requested.Where(id => !receivedIds.Contains(id)).ToList();
    }
}

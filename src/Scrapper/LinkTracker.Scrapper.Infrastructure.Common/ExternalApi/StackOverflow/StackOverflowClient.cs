using FluentResults;
using LinkTracker.Scrapper.Application.Scraping.ApiClients.StackOverflow;
using LinkTracker.Scrapper.Application.Scraping.ApiClients.StackOverflow.Models;
using Microsoft.Extensions.Logging;

namespace LinkTracker.Scrapper.Infrastructure.Pipelines.ExternalApi.StackOverflow;

public class StackOverflowClient(IStackOverflowRefitClient refitClient, ILogger<StackOverflowClient> logger) : IStackOverflowClient
{
    public async Task<Result<AnswersResponse>> GetAnswers(long[] questionIds, int page, int limit)
    {
        var questions = string.Join(';', questionIds);
        var res = await refitClient.GetAnswers(questions, page, limit);

        if (res is { IsSuccessful: true, Content: not null })
        {
            return Result.Ok(new AnswersResponse(
                res.Content.Answers.Select(a => new AnswerResponse(a.QuestionId, a.AnswerId, a.Body, a.User.Name, DateTime.FromBinary(a.LastActivityDate))).ToArray(),
                res.Content.HasMore
                ));
        }

        logger.LogError("StackOverflow API error: {error}", res.Error);

        return Result.Fail(res.Error?.ReasonPhrase ?? "Unknown API Error");
    }

    public async Task<Result<CommentsResponse>> GetComments(long[] answerIds, int page, int limit)
    {
        var answers = string.Join(';', answerIds);
        var res = await refitClient.GetComments(answers, page, limit);

        if (res is { IsSuccessful: true, Content: not null })
        {
            return Result.Ok(new CommentsResponse(
                res.Content.Comments.Select(c => new CommentResponse(c.CommentId, c.PostId, c.Body, c.User.Name, DateTime.FromBinary(c.CreationDate))).ToArray(),
                res.Content.HasMore
            ));
        }

        logger.LogError("StackOverflow API error: {error}", res.Error);

        return Result.Fail(res.Error?.ReasonPhrase ?? "Unknown API Error");
    }

    public async Task<Result<CommentsResponse>> GetQuestionComments(long[] questionIds, int page, int limit)
    {
        var questions = string.Join(';', questionIds);
        var res = await refitClient.GetQuestionComments(questions, page, limit);

        if (res is { IsSuccessful: true, Content: not null })
        {
            return Result.Ok(new CommentsResponse(
                res.Content.Comments.Select(c => new CommentResponse(c.CommentId, c.PostId, c.Body, c.User.Name, DateTime.FromBinary(c.CreationDate))).ToArray(),
                res.Content.HasMore
            ));
        }

        logger.LogError("StackOverflow API error: {error}", res.Error);

        return Result.Fail(res.Error?.ReasonPhrase ?? "Unknown API Error");
    }

    public async Task<Result<QuestionsResponse>> GetQuestions(long[] questionIds, int page, int limit)
    {
        var questions = string.Join(';', questionIds);
        var res = await refitClient.GetQuestions(questions, page, limit);

        if (res is { IsSuccessful: true, Content: not null })
        {
            return Result.Ok(new QuestionsResponse(
                res.Content.Questions.Select(c => new QuestionResponse(c.QuestionId, c.Title)).ToArray(),
                res.Content.HasMore
            ));
        }
        logger.LogError("StackOverflow API error: {error}", res.Error);

        return Result.Fail(res.Error?.ReasonPhrase ?? "Unknown API Error");
    }
}

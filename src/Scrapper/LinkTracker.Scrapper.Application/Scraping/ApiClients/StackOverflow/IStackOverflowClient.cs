using FluentResults;
using LinkTracker.Scrapper.Application.Scraping.ApiClients.StackOverflow.Models;

namespace LinkTracker.Scrapper.Application.Scraping.ApiClients.StackOverflow;

public interface IStackOverflowClient
{
    Task<Result<AnswersResponse>> GetAnswers(long[] questionIds, int page, int limit);
    Task<Result<CommentsResponse>> GetComments(long[] answerIds, int page, int limit);
    Task<Result<CommentsResponse>> GetQuestionComments(long[] questionIds, int page, int limit);
    Task<Result<QuestionsResponse>> GetQuestions(long[] questionIds, int page, int limit);
}

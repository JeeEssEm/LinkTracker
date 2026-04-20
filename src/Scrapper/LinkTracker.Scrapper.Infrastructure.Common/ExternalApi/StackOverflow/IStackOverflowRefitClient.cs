using LinkTracker.Scrapper.Infrastructure.Pipelines.ExternalApi.StackOverflow.Dtos;
using Refit;

namespace LinkTracker.Scrapper.Infrastructure.Pipelines.ExternalApi.StackOverflow;

public interface IStackOverflowRefitClient
{
    [Headers("User-Agent: refit-client")]
    [Get("/2.3/questions/{questionIds}/answers?site=stackoverflow&filter=withbody&page={page}&limit={limit}")]
    Task<ApiResponse<AnswersResponseDto>> GetAnswers(string questionIds, int page, int limit);

    [Headers("User-Agent: refit-client")]
    [Get("/2.3/answers/{commentIds}/comments?site=stackoverflow&filter=withbody&page={page}&limit={limit}")]
    Task<ApiResponse<CommentsResponseDto>> GetComments(string commentIds, int page, int limit);

    [Headers("User-Agent: refit-client")]
    [Get("/2.3/questions/{questionIds}/comments?site=stackoverflow&filter=withbody&page={page}&limit={limit}")]
    Task<ApiResponse<CommentsResponseDto>> GetQuestionComments(string questionIds, int page, int limit);

    [Headers("User-Agent: refit-client")]
    [Get("/2.3/questions/{questionIds}?site=stackoverflow&page={page}&limit={limit}")]
    Task<ApiResponse<QuestionsResponseDto>> GetQuestions(string questionIds, int page, int limit);
}

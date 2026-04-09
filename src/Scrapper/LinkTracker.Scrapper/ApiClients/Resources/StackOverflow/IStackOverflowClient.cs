using Refit;

namespace LinkTracker.Scrapper.ApiClients.Resources.StackOverflow;

public interface IStackOverflowClient
{
    [Get("/2.3/questions/{questionId}?site=stackoverflow")]
    Task<ApiResponse<StackOverflowResponse>> GetQuestionAsync(long questionId);
}

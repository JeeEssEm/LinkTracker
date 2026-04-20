using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Data;

namespace LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Results;

public record FetchQuestionCommentsResult(
    IReadOnlyList<QuestionCommentData> ReceivedComments,
    IReadOnlyList<long> MissingQuestionIds
);

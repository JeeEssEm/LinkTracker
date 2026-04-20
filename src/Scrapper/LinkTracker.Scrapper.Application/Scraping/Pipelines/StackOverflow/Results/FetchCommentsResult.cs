using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Data;

namespace LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Results;

public record FetchCommentsResult(
    IReadOnlyList<CommentData> FoundComments,
    IReadOnlyList<long> MissingAnswerIds
    );

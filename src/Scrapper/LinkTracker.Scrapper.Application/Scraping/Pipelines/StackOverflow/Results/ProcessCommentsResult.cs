using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Data;

namespace LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Results;

public record ProcessCommentsResult(List<CommentData> NewComments, List<long> FailedLinkIds);

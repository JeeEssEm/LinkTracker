namespace LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Data;

public record CommentData(
    long Id,
    AnswerData Answer,
    string Body,
    string OwnerDisplayName,
    DateTime CreatedAt
);

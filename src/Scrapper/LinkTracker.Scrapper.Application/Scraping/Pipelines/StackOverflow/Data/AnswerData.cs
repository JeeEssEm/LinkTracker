namespace LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Data;

public record AnswerData(
    long Id,
    QuestionData Question,
    string Body,
    string OwnerDisplayName,
    DateTime CreatedAt
);

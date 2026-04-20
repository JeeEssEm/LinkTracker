using LinkTracker.Scrapper.Application.Scraping.Pipelines.Shared.Data;

namespace LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Data;

public record QuestionData(long Id, LinkMetadata Link, string Title = "");

using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Data;

namespace LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Results;

public record FetchAnswersResult(IReadOnlyList<AnswerData> FoundAnswers, IReadOnlyList<long> MissingQuestionIds);

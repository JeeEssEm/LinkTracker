using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Data;

namespace LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Results;

public record ProcessAnswersResult(IReadOnlyList<AnswerData> AllAnswers, IReadOnlyList<AnswerData> NewAnswers, List<long> FailedLinkIds);

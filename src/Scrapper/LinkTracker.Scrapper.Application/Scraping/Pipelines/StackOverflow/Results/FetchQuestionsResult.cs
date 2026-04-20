using LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Data;

namespace LinkTracker.Scrapper.Application.Scraping.Pipelines.StackOverflow.Results;

public record FetchQuestionsResult(IReadOnlyList<QuestionData> FoundQuestions, IReadOnlyList<QuestionData> BannedQuestions);

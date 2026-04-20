namespace LinkTracker.Scrapper.Application.Scraping.ApiClients.StackOverflow.Models;

public record QuestionsResponse(IReadOnlyList<QuestionResponse> Questions, bool HasMore);

namespace LinkTracker.Scrapper.Application.Scraping.ApiClients.StackOverflow.Models;

public record AnswersResponse(IReadOnlyList<AnswerResponse> Answers, bool HasMore);

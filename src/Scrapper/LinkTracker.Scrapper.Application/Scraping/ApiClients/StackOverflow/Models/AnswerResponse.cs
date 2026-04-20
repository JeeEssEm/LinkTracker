namespace LinkTracker.Scrapper.Application.Scraping.ApiClients.StackOverflow.Models;

public record AnswerResponse(long QuestionId, long AnswerId, string Body, string Username, DateTime CreatedAt);

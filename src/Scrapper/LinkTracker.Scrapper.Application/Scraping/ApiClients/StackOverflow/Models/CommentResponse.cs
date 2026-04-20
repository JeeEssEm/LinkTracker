namespace LinkTracker.Scrapper.Application.Scraping.ApiClients.StackOverflow.Models;

public record CommentResponse(long CommentId, long PostId, string Body, string Username, DateTime CreationDate);

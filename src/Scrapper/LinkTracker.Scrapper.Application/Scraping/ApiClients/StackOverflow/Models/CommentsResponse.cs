namespace LinkTracker.Scrapper.Application.Scraping.ApiClients.StackOverflow.Models;

public record CommentsResponse(IReadOnlyList<CommentResponse> Comments, bool HasMore);

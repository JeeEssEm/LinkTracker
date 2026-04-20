using LinkTracker.Scrapper.Application.Scraping.ApiClients.Github.Models.Statuses;

namespace LinkTracker.Scrapper.Application.Scraping.ApiClients.Github.Models;

public record IssueResponse(string Title, int Number, string Username, DateTime CreatedAt, string Description, IssueStatus Status);

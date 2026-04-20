using LinkTracker.Scrapper.Application.Scraping.ApiClients.Github.Models.Statuses;

namespace LinkTracker.Scrapper.Application.Scraping.ApiClients.Github.Models;

public record PullRequestResponse(string Title, int Number, string Description, string Username, DateTime CreatedAt, PullRequestStatus Status);

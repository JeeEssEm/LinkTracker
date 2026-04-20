using LinkTracker.Scrapper.Application.Scraping.ApiClients.Github.Models.Statuses;

namespace LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Data;

public record IssueData(RepositoryData Repository, int Number, string Title, string Username, DateTime CreatedAt, string Description, IssueStatus Status);

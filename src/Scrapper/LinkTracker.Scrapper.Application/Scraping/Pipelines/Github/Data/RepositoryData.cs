using LinkTracker.Scrapper.Application.Scraping.Pipelines.Shared.Data;

namespace LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Data;

public record RepositoryData(int Id, LinkMetadata Link, string Owner, string Repo);

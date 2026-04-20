namespace LinkTracker.Scrapper.Application.Scraping.Pipelines.Github.Store.ReadModels;

public record RepositoryModel(int Id, long LinkId, string Owner, string Name);
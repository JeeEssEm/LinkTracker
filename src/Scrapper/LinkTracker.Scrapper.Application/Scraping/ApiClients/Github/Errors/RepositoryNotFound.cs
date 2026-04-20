using FluentResults;

namespace LinkTracker.Scrapper.Application.Scraping.ApiClients.Github.Errors;

public class RepositoryNotFound(string Owner, string Repo) : Error;

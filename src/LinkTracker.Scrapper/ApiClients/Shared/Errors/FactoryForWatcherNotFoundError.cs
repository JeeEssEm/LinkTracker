using FluentResults;

namespace LinkTracker.Scrapper.ApiClients.Shared.Errors;

public class FactoryForWatcherNotFoundError() : Error("Cannot create watcher for provided link");
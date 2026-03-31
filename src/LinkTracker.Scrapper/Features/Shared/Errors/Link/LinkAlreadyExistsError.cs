using FluentResults;

namespace LinkTracker.Scrapper.Features.Shared.Errors.Link;

public class LinkAlreadyExistsError() : Error("Link already exists");
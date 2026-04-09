using FluentResults;

namespace LinkTracker.Scrapper.Contracts.Errors;

public class IncorrectRequestParameters(string message) : Error(message);
using LinkTracker.Scrapper.Domain.Tracking.ValueObjects;

namespace LinkTracker.Scrapper.Application.Dtos.Links;

public record LinkDto(Guid Id, string Url, LinkSourceType SourceType);

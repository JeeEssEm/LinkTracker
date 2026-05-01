using LinkTracker.Scrapper.Application.ReadModels;
using LinkTracker.Scrapper.Contracts.Dtos.Links;
using LinkTracker.Scrapper.Domain.Tracking.Entities;

namespace LinkTracker.Scrapper.Application.QueryServices;

public interface ISubscriptionQueryService
{
    Task<IReadOnlyList<LinkResponse>> ListSubscriptionsByChatAsync(ChatId chatId);
}

using LinkTracker.Scrapper.Domain.Tracking.Entities;

namespace LinkTracker.Scrapper.Domain.Tracking.Repositories;

public interface IChatRepository
{
    Task<bool> AddChatAsync(ChatId telegramId);
    Task<Chat?> GetChatAsync(ChatId id);
    Task<bool> RemoveChatAsync(ChatId id);
    Task<bool> ExistsAsync(ChatId id);
}

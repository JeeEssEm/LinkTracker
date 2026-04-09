using FluentResults;
using LinkTracker.Scrapper.Domain;
using LinkTracker.Scrapper.Domain.Chats.Entities;

namespace LinkTracker.Scrapper.Application.Repositories;

public interface IChatRepository
{
    Task<bool> AddChatAsync(Chat chat);
    Task<Chat?> GetChatAsync(long id);
    Task RemoveChatAsync(long id);
}

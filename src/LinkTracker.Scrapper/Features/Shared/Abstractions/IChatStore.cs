using FluentResults;
using LinkTracker.Scrapper.Domain;

namespace LinkTracker.Scrapper.Features.Shared.Abstractions;

public interface IChatStore
{
    Task<Result> AddChatAsync(Chat chat);
    Task<Result<Chat>> GetChatAsync(long id);
    Task<Result> RemoveChatAsync(long id);
}
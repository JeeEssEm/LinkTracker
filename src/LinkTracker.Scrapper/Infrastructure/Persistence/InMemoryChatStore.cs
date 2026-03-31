using FluentResults;
using LinkTracker.Scrapper.Domain;
using LinkTracker.Scrapper.Features.Shared.Abstractions;

namespace LinkTracker.Scrapper.Infrastructure.Persistence;

public class InMemoryChatStore(InMemoryDataStorage storage) : IChatStore
{
    public Task<Result> AddChatAsync(Chat chat)
    {
        if (!storage.Chats.TryAdd(chat.Id, chat))
        {
            return Task.FromResult(Result.Fail("Chat already exists"));
        }

        return Task.FromResult(Result.Ok());
    }

    public Task<Result<Chat>> GetChatAsync(long id)
    {
        if (storage.Chats.TryGetValue(id, out var chat))
        {
            return Task.FromResult(Result.Ok(chat));
        }

        return Task.FromResult(Result.Fail<Chat>("Chat not found"));
    }

    public Task<Result> RemoveChatAsync(long id)
    {
        if (storage.Chats.TryRemove(id, out _))
        {
            return Task.FromResult(Result.Ok());
        }

        return Task.FromResult(Result.Fail("Chat not found"));
    }
}

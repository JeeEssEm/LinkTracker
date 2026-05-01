using FluentResults;
using LinkTracker.Scrapper.Domain.Tracking.Entities;
using LinkTracker.Scrapper.Domain.Tracking.Repositories;
using LinkTracker.Scrapper.Errors.Chats;

namespace LinkTracker.Scrapper.Application.Handlers.Chats;

public class AddChatHandler(IChatRepository chatRepository)
{
    public async Task<Result> HandleAsync(long chatId)
    {
        var added = await chatRepository.AddChatAsync(new ChatId(chatId));
        return !added ? Result.Fail(new ChatAlreadyExists()) : Result.Ok();
    }
}

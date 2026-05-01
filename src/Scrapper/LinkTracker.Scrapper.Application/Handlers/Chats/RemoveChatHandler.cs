using FluentResults;
using LinkTracker.Scrapper.Domain.Tracking.Entities;
using LinkTracker.Scrapper.Domain.Tracking.Repositories;
using LinkTracker.Scrapper.Errors.Chats;

namespace LinkTracker.Scrapper.Application.Handlers.Chats;

public class RemoveChatHandler(IChatRepository chatRepository)
{
    public async Task<Result> HandleAsync(long chatId)
    {
        var removed = await chatRepository.RemoveChatAsync(new ChatId(chatId));
        return removed ? Result.Ok() : Result.Fail(new ChatNotFound());
    }
}

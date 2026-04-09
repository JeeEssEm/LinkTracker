using FluentResults;
using LinkTracker.Scrapper.Application.Repositories;
using LinkTracker.Scrapper.Errors.Chats;

namespace LinkTracker.Scrapper.Features.Chats.RemoveChat;

public class RemoveChatHandler(IChatRepository chatRepository)
{
    public async Task<Result> HandleAsync(long chatId)
    {
        var chat = await chatRepository.GetChatAsync(chatId);
        if (chat == null)
        {
            return Result.Fail(new ChatNotFound());
        }
        await chatRepository.RemoveChatAsync(chatId);
        return Result.Ok();
    }
}
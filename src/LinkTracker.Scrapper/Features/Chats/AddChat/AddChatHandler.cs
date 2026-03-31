using FluentResults;
using LinkTracker.Scrapper.Features.Shared.Abstractions;
using LinkTracker.Scrapper.Features.Shared.Errors.Chat;

namespace LinkTracker.Scrapper.Features.Chats.AddChat;

public class AddChatHandler(IChatStore chatStore)
{
    public async Task<Result> HandleAsync(long chatId)
    {
        var chat = new Domain.Chat(chatId);
        var resGetChat = await chatStore.GetChatAsync(chatId);

        if (resGetChat.IsSuccess)
        {
            return Result.Fail(new ChatAlreadyExistsError());
        }

        return await chatStore.AddChatAsync(chat);
    }
}

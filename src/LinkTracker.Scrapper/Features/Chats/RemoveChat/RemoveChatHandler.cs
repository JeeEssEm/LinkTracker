using FluentResults;
using LinkTracker.Scrapper.Features.Shared.Abstractions;
using LinkTracker.Scrapper.Features.Shared.Errors.Chat;

namespace LinkTracker.Scrapper.Features.Chats.RemoveChat;

public class RemoveChatHandler(IChatStore chatStore)
{
    public async Task<Result> HandleAsync(long chatId)
    {
        var resGetChat = await chatStore.GetChatAsync(chatId);
        if (resGetChat.IsFailed)
        {
            return Result.Fail(new ChatNotFoundError());
        }

        return await chatStore.RemoveChatAsync(chatId);
    }
}
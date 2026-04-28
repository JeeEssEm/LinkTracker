using System.Threading.Channels;
using FluentResults;
using LinkTracker.Scrapper.Application.Abstractions;
using LinkTracker.Scrapper.Application.Repositories;
using LinkTracker.Scrapper.Application.Scraping.Pipelines.Notifications;
using LinkTracker.Scrapper.Domain.Chats.Entities;
using LinkTracker.Scrapper.Errors.Chats;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace LinkTracker.Scrapper.Features.Chats.AddChat;

public class AddChatHandler(IChatRepository chatRepository)
{
    public async Task<Result> HandleAsync(long chatId)
    {
        var chat = new Chat(chatId);
        var added = await chatRepository.AddChatAsync(chat);
        return !added ? Result.Fail(new ChatAlreadyExists()) : Result.Ok();
    }
}

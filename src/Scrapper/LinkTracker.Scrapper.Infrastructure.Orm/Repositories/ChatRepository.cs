using System.Data.Common;
using FluentResults;
using LinkTracker.Scrapper.Application.Repositories;
using LinkTracker.Scrapper.Domain.Chats.Entities;
using LinkTracker.Scrapper.Errors.Chats;
using LinkTracker.Scrapper.Infrastructure.Orm.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LinkTracker.Scrapper.Infrastructure.Orm.Repositories;

public class ChatRepository(LinkTrackerContext dbContext, ILogger<ChatRepository> logger) : IChatRepository
{
    public async Task<bool> AddChatAsync(Chat chat)
    {
        var chatModel = new Models.Chat() { Id = chat.Id };

        try
        {
            await dbContext.AddAsync(chatModel);
            await dbContext.SaveChangesAsync();

            return true;
        }
        catch (DbException e)
        {
            logger.LogError(e, "Failed to add chat");
            return false;
        }
    }

    public async Task<Chat?> GetChatAsync(long id)
    {
        var existingChat = await dbContext.Chats.FindAsync(id);
        return existingChat == null ? null : new Chat(existingChat.Id);
    }

    public async Task RemoveChatAsync(long id)
    {
        var chat = await dbContext.Chats.FindAsync(id);

        if (chat != null)
        {
            dbContext.Chats.Remove(chat);
            await dbContext.SaveChangesAsync();
        }
    }
}
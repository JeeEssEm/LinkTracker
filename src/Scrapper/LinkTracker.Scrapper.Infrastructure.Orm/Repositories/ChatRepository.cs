using System.Data.Common;
using FluentResults;
using LinkTracker.Scrapper.Application.Repositories;
using LinkTracker.Scrapper.Domain.Tracking.Entities;
using LinkTracker.Scrapper.Domain.Tracking.Repositories;
using LinkTracker.Scrapper.Errors.Chats;
using LinkTracker.Scrapper.Infrastructure.Orm.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LinkTracker.Scrapper.Infrastructure.Orm.Repositories;

public class ChatRepository(LinkTrackerContext dbContext, ILogger<ChatRepository> logger) : IChatRepository
{
    public async Task<bool> AddChatAsync(ChatId telegramId)
    {
        if (await dbContext.Chats.CountAsync(c => c.Id == telegramId.Value) != 0)
        {
            return false;
        }
        var chatModel = new Models.Chat { Id = telegramId.Value };

        await dbContext.AddAsync(chatModel);
        await dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<Chat?> GetChatAsync(ChatId id)
    {
        var existingChat = await dbContext.Chats.FindAsync(id.Value);
        return existingChat == null ? null : new Chat(id);
    }

    public async Task<bool> RemoveChatAsync(ChatId id)
    {
        var chat = await dbContext.Chats.FindAsync(id.Value);

        if (chat != null)
        {
            dbContext.Chats.Remove(chat);
            await dbContext.SaveChangesAsync();
            return true;
        }

        return false;
    }

    public async Task<bool> ExistsAsync(ChatId id)
    {
        return await dbContext.Chats.CountAsync(c => c.Id == id.Value) != 0;
    }
}

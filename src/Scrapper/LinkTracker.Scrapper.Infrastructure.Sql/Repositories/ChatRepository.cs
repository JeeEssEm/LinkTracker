using Dapper;
using FluentResults;
using LinkTracker.Scrapper.Application.Repositories;
using LinkTracker.Scrapper.Domain.Chats.Entities;
using LinkTracker.Scrapper.Errors.Chats;

namespace LinkTracker.Scrapper.Infrastructure.Sql.Repositories;

public class ChatRepository(DatabaseSession dbSession) : IChatRepository
{
    public async Task<bool> AddChatAsync(Chat chat)
    {
        var dbConnection = await dbSession.GetConnectionAsync();
        var affectedRows = await dbConnection.ExecuteAsync(
            """
            INSERT INTO chats(id)
            VALUES (@Id)
            ON CONFLICT DO NOTHING
            """, new { chat.Id }, transaction: dbSession.Transaction);
        return affectedRows != 0;
    }

    public async Task<Chat?> GetChatAsync(long id)
    {
        var dbConnection = await dbSession.GetConnectionAsync();
        var chat = await dbConnection.QuerySingleOrDefaultAsync<Models.Chat>(
            """
            SELECT * FROM chats WHERE id = @Id
            LIMIT 1
            """, new { id }, transaction: dbSession.Transaction);
        if (chat != null)
        {
            return new Chat(chat.Id);
        }

        return null;
    }

    public async Task RemoveChatAsync(long id)
    {
        var dbConnection = await dbSession.GetConnectionAsync();
        await dbConnection.ExecuteAsync(
            """
            DELETE FROM chats WHERE id = @Id
            """, new { id }, transaction: dbSession.Transaction);
    }
}
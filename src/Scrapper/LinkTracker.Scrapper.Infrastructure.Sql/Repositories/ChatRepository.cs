using Dapper;
using LinkTracker.Scrapper.Domain.Tracking.Entities;
using LinkTracker.Scrapper.Domain.Tracking.Repositories;
using LinkTracker.Scrapper.Infrastructure.Pipelines.Database;

namespace LinkTracker.Scrapper.Infrastructure.Sql.Repositories;

public class ChatRepository(DatabaseSession dbSession) : IChatRepository
{
    public async Task<bool> AddChatAsync(ChatId telegramId)
    {
        var dbConnection = await dbSession.OpenConnectionAsync();
        var affectedRows = await dbConnection.ExecuteAsync(
            """
            INSERT INTO chats(id)
            VALUES (@id)
            ON CONFLICT DO NOTHING
            """, new { id = telegramId.Value }, transaction: dbSession.Transaction);
        return affectedRows != 0;
    }

    public async Task<Chat?> GetChatAsync(ChatId id)
    {
        var dbConnection = await dbSession.OpenConnectionAsync();
        var chat = await dbConnection.QuerySingleOrDefaultAsync<long?>(
            """
            SELECT * FROM chats WHERE id = @Id
            LIMIT 1
            """, new { id = id.Value }, transaction: dbSession.Transaction);

        if (chat != null)
        {
            return new Chat(id);
        }

        return null;
    }

    public async Task<bool> RemoveChatAsync(ChatId id)
    {
        var dbConnection = await dbSession.OpenConnectionAsync();
        var affectedRows = await dbConnection.ExecuteAsync(
            """
            DELETE FROM chats WHERE id = @id
            """, new { id = id.Value }, transaction: dbSession.Transaction);
        return affectedRows != 0;
    }

    public async Task<bool> ExistsAsync(ChatId id)
    {
        var dbConnection = await dbSession.OpenConnectionAsync();
        var count = await dbConnection.QuerySingleAsync<int>(
            """
            SELECT COUNT(*) FROM chats WHERE id = @id
            """, new { id = id.Value }, transaction: dbSession.Transaction);
        return count != 0;
    }
}

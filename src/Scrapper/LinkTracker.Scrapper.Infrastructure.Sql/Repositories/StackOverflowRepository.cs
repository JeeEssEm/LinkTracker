using Dapper;
using LinkTracker.Scrapper.Domain.Tracking.Entities;
using LinkTracker.Scrapper.Domain.Tracking.SourceHandlers.StackOverflow;
using LinkTracker.Scrapper.Infrastructure.Pipelines.Database;

namespace LinkTracker.Scrapper.Infrastructure.Sql.Repositories;

public class StackOverflowRepository(DatabaseSession dbSession) : IStackOverflowRepository
{
    public async Task CreateQuestionAsync(LinkId linkId, long questionId)
    {
        var dbConnection = await dbSession.OpenConnectionAsync();
        var query = "INSERT INTO stackoverflow_questions(id, link_id) VALUES (@id, @linkId) ON CONFLICT DO NOTHING";
        await dbConnection.ExecuteAsync(query, new { id = questionId, linkId = linkId.Value }, transaction: dbSession.Transaction);
    }
}

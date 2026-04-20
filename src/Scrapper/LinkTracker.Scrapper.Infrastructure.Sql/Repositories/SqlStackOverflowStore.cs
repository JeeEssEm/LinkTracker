using Dapper;
using LinkTracker.Scrapper.Infrastructure.Pipelines.Repositories;

namespace LinkTracker.Scrapper.Infrastructure.Sql.Repositories;

internal class SqlStackOverflowStore(DatabaseSession session) : AbstractStackOverflowStore
{
    protected override async Task<long[]> ExecuteQueryAsync(string sql, object parameters)
    {
        var connection = await session.GetConnectionAsync();
        var result = await connection.QueryAsync<long>(sql, parameters, transaction: session.Transaction);
        return result.ToArray();
    }

    protected override async Task ExecuteAsync(string sql, object parameters)
    {
        var connection = await session.GetConnectionAsync();
        await connection.ExecuteAsync(sql, parameters, transaction: session.Transaction);
    }

    protected override async Task<Dictionary<long, long>> GetQuestionIdsByLinkIdsAsyncInternal(IEnumerable<long> linkIds)
    {
        const string sql = "SELECT link_id as Key, id as Value FROM stackoverflow_questions WHERE link_id = ANY(@LinkIds)";
        var connection = await session.GetConnectionAsync();
        var results = await connection.QueryAsync<KeyValuePair<long, long>>(sql, new { LinkIds = linkIds.ToArray() }, transaction: session.Transaction);

        return results.ToDictionary(k => k.Key, v => v.Value);
    }
}

using Dapper;
using LinkTracker.Scrapper.Infrastructure.Orm.Context;
using LinkTracker.Scrapper.Infrastructure.Pipelines.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace LinkTracker.Scrapper.Infrastructure.Orm.Repositories;

public class OrmStackOverflowStore(LinkTrackerContext context) : AbstractStackOverflowStore
{
    protected override async Task<long[]> ExecuteQueryAsync(string sql, object parameters)
    {
        var connection = context.Database.GetDbConnection();
        var transaction = context.Database.CurrentTransaction?.GetDbTransaction();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }
        var result = await connection.QueryAsync<long>(sql, parameters, transaction);
        return result.ToArray();
    }

    protected override async Task ExecuteAsync(string sql, object parameters)
    {
        var connection = context.Database.GetDbConnection();
        var transaction = context.Database.CurrentTransaction?.GetDbTransaction();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }
        await connection.ExecuteAsync(sql, parameters, transaction);
    }

    protected override async Task<Dictionary<long, long>> GetQuestionIdsByLinkIdsAsyncInternal(IEnumerable<long> linkIds)
    {
        const string sql = "SELECT link_id as Key, id as Value FROM stackoverflow_questions WHERE link_id = ANY(@LinkIds)";
        var connection = context.Database.GetDbConnection();
        var transaction = context.Database.CurrentTransaction?.GetDbTransaction();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        var results = await connection.QueryAsync<KeyValuePair<long, long>>(sql, new { LinkIds = linkIds.ToArray() }, transaction);
        return results.ToDictionary(k => k.Key, v => v.Value);
    }
}

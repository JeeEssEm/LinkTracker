using System.Data;
using System.Text.Json;
using Dapper;
using LinkTracker.Scrapper.Application.Dtos.Outbox;
using LinkTracker.Scrapper.Application.Repositories;
using LinkTracker.Scrapper.Infrastructure.Orm.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using DbOutboxEvent = LinkTracker.Scrapper.Infrastructure.Orm.Models.OutboxEvent;

namespace LinkTracker.Scrapper.Infrastructure.Orm.Repositories;

public class OrmOutboxRepository(LinkTrackerContext context) : IOutboxEventRepository
{
    public async Task CreateAsync(IEnumerable<CreateOutboxEvent> outboxEvents)
    {
        var entities = outboxEvents.Select(e => new DbOutboxEvent
        {
            Payload = e.Payload,
            RetryCount = 0,
            EventId = e.EventId,
            Status = e.Status,
            AttemptedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await context.OutboxEvents.AddRangeAsync(entities);
        await context.SaveChangesAsync();
    }

    public async Task SetSent(IEnumerable<long> ids)
    {
        var idList = ids.ToList();
        await context.OutboxEvents
            .Where(e => idList.Contains(e.Id))
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.Status, EventStatus.Sent)
                .SetProperty(e => e.AttemptedAt, DateTimeOffset.UtcNow));
    }

    public async Task SetFailed(IEnumerable<(long eventId, string error)> events)
    {
        var dbConnection = context.Database.GetDbConnection();
        var transaction = context.Database.CurrentTransaction?.GetDbTransaction();

        var query =
            """
            UPDATE outbox_events AS events
            SET 
                status = @status,
                attempted_at = now(),
                error_messages = error_messages || jsonb_build_array(temp.error_msg::text),
                retry_count = retry_count + 1
            FROM UNNEST(@ids, @errors) AS temp(id, error_msg)
            WHERE events.id = temp.id
            """;

        var valueTuples = events.ToArray();
        var parameters = new { ids = valueTuples.Select(e => e.eventId).ToArray(), errors = valueTuples.Select(e => e.error).ToArray(), status = (int)EventStatus.Failed };

        await dbConnection.ExecuteAsync(query, parameters, transaction: transaction);
    }

    public async Task<OutboxEvent[]> GetEventsToProcess(int batchSize, int retryCountLimit, TimeSpan? retryDelay = null)
    {
        var retryDelaySec = 2 * 60;
        if (retryDelay.HasValue)
        {
            retryDelaySec = (int)retryDelay.Value.TotalSeconds;
        }

        var query =
            """
            UPDATE outbox_events
                SET status = @processingStatus,
                    attempted_at = now()
            WHERE id IN (
                SELECT id FROM outbox_events
                WHERE status != @doneStatus AND retry_count < @retryCountLimit AND (status != @processingStatus OR now() > attempted_at + (@retryTimeout * interval '1 second'))
                ORDER BY created_at
                LIMIT @batchSize
                FOR UPDATE SKIP LOCKED
                )
            RETURNING *;            
            """;

        var res = await context.Database.SqlQueryRaw<DbOutboxEvent>(
            query,
            new NpgsqlParameter("processingStatus", (int)EventStatus.Processing),
            new NpgsqlParameter("batchSize", batchSize),
            new NpgsqlParameter("retryCountLimit", retryCountLimit),
            new NpgsqlParameter("retryTimeout", retryDelaySec),
            new NpgsqlParameter("doneStatus", (int)EventStatus.Sent))
        .ToArrayAsync();

        return res.Select(e => new OutboxEvent(
            e.Id,
            e.Headers,
            e.Payload,
            e.RetryCount,
            e.ErrorMessages,
            e.EventId,
            e.Status,
            e.AttemptedAt.UtcDateTime,
            e.CreatedAt.UtcDateTime
        )).ToArray();
    }
}

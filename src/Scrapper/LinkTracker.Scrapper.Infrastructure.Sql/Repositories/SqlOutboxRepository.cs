using System.Text.Json;
using Dapper;
using LinkTracker.Scrapper.Application.Dtos.Outbox;
using LinkTracker.Scrapper.Application.Repositories;
using LinkTracker.Scrapper.Infrastructure.Pipelines.Database;

namespace LinkTracker.Scrapper.Infrastructure.Sql.Repositories;

public class SqlOutboxRepository(DatabaseSession session) : IOutboxEventRepository
{
    public async Task CreateAsync(IEnumerable<CreateOutboxEvent> outboxEvents)
    {
        var dbConnection = await session.OpenConnectionAsync();
        var query =
            """
            INSERT INTO outbox_events(headers, payload, retry_count, error_messages, event_id, status, attempted_at, created_at) 
            SELECT 
                h::jsonb,
                p, 0, '[]'::jsonb, id, s, now(), now()
            FROM UNNEST(
                @headers,
                @payload,
                @eventId,
                @status
            ) AS t(h, p, id, s)
            """;

        var events = outboxEvents as CreateOutboxEvent[] ?? outboxEvents.ToArray();

        await dbConnection.ExecuteAsync(query, new
        {
            headers = events.Select(e => e.Headers).ToArray(),
            payload = events.Select(e => e.Payload).ToArray(),
            eventId = events.Select(e => (short)e.EventId).ToArray(),
            status = events.Select(e => (short)e.Status).ToArray()
        }, transaction: session.Transaction);
    }

    public async Task SetSent(IEnumerable<long> ids)
    {
        var dbConnection = await session.OpenConnectionAsync();
        var query =
            """
            UPDATE outbox_events
                SET status = @status,
                    attempted_at = now()
            WHERE id = ANY(@ids)
            """;
        await dbConnection.ExecuteAsync(query, new { ids = ids.ToArray(), status = EventStatus.Sent }, transaction: session.Transaction);
    }

    public async Task SetFailed(IEnumerable<(long eventId, string error)> events)
    {
        var dbConnection = await session.OpenConnectionAsync();
        var query =
            """
            UPDATE outbox_events AS events
            SET 
                status = @status,
                attempted_at = now(),
                error_messages = error_messages || jsonb_build_array(temp.error_msg),
                retry_count = retry_count + 1
            FROM UNNEST(@ids, @errors) AS temp(id, error_msg)
            WHERE events.id = temp.id
            """;

        var valueTuples = events as (long eventId, string error)[] ?? events.ToArray();
        var parameters = new { ids = valueTuples.Select(e => e.eventId).ToArray(), errors = valueTuples.Select(e => e.error).ToArray(), status = (short)EventStatus.Failed };
        await dbConnection.ExecuteAsync(query, parameters, transaction: session.Transaction);
    }

    public async Task<OutboxEvent[]> GetEventsToProcess(int batchSize, int retryCountLimit, TimeSpan? retryDelay = null)
    {
        var retryDelaySec = 2 * 60;
        if (retryDelay.HasValue)
        {
            retryDelaySec = (int)retryDelay.Value.TotalSeconds;
        }

        var dbConnection = await session.OpenConnectionAsync();
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

        var res = await dbConnection.QueryAsync<Models.OutboxEvent>(query,
            new
            {
                processingStatus = EventStatus.Processing,
                batchSize,
                retryCountLimit,
                retryTimeout = retryDelaySec,
                doneStatus = EventStatus.Sent
            });
        return res.Select(e => new OutboxEvent(
            e.Id,
            JsonSerializer.Deserialize<Dictionary<string, string>>(e.Headers) ?? [],
            e.Payload,
            e.RetryCount,
            JsonSerializer.Deserialize<List<string>>(e.ErrorMessages) ?? [],
            e.EventId,
            e.Status,
            e.AttemptedAt,
            e.CreatedAt
        )).ToArray();
    }
}

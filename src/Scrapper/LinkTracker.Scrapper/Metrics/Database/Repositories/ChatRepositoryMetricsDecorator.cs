using System.Diagnostics;
using LinkTracker.Scrapper.Domain.Tracking.Entities;
using LinkTracker.Scrapper.Domain.Tracking.Repositories;

namespace LinkTracker.Scrapper.Metrics.Database.Repositories;

public class ChatRepositoryMetricsDecorator(IChatRepository chatRepo, ApplicationMetrics metrics) : IChatRepository
{
    public async Task<bool> AddChatAsync(ChatId telegramId)
    {
        var sw = Stopwatch.StartNew();
        var result = await chatRepo.AddChatAsync(telegramId);
        metrics.RequestDuration.Record(
            sw.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("scope", RequestDurationScopes.Database),
            new KeyValuePair<string, object?>("scope_type", "chats")
            );
        return result;
    }

    public async Task<Chat?> GetChatAsync(ChatId id)
    {
        var sw = Stopwatch.StartNew();
        var result = await chatRepo.GetChatAsync(id);
        metrics.RequestDuration.Record(
            sw.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("scope", RequestDurationScopes.Database),
            new KeyValuePair<string, object?>("scope_type", "chats")
        );
        return result;
    }

    public async Task<bool> RemoveChatAsync(ChatId id)
    {
        var sw = Stopwatch.StartNew();
        var result = await chatRepo.RemoveChatAsync(id);
        metrics.RequestDuration.Record(
            sw.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("scope", RequestDurationScopes.Database),
            new KeyValuePair<string, object?>("scope_type", "chats")
        );
        return result;
    }

    public async Task<bool> ExistsAsync(ChatId id)
    {
        var sw = Stopwatch.StartNew();
        var result = await chatRepo.ExistsAsync(id);
        metrics.RequestDuration.Record(
            sw.Elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("scope", RequestDurationScopes.Database),
            new KeyValuePair<string, object?>("scope_type", "chats")
        );
        return result;
    }
}
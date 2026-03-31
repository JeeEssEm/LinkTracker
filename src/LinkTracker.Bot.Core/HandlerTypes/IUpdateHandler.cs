using Telegram.Bot.Types;

namespace LinkTracker.Bot.Core.HandlerTypes;

public interface IUpdateHandler
{
    Task HandleAsync(Update update, CancellationToken ct);
}
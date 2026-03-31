using Telegram.Bot.Types;

namespace LinkTracker.Bot.Core.HandlerTypes;

public interface IMessageHandler
{
    Task HandleAsync(Message msg, CancellationToken ct);
}

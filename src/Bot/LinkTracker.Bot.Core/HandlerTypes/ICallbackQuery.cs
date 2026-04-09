using Telegram.Bot.Types;

namespace LinkTracker.Bot.Core.HandlerTypes;

public interface ICallbackQuery
{
    Task HandleAsync(CallbackQuery callbackQuery, CancellationToken ct);
}
using LinkTracker.Bot.Core.StateMachine;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace LinkTracker.Bot.Core.Middlewares;

public class UpdateContext
{
    public UserSession UserSession { get; init; }
    public ITelegramBotClient BotClient { get; init; }
    public CancellationToken CancellationToken { get; init; }
    public IServiceProvider Services { get; init; }
    public Update Update { get; init; }
}
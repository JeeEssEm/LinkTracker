using LinkTracker.Bot.Core.HandlerTypes;
using LinkTracker.Bot.Core.HandlerTypes.Attributes;
using LinkTracker.Bot.Core.StateMachine.Attributes;
using Telegram.Bot.Types;

namespace LinkTracker.Bot.App.Features.Bot.TrackLink.Flow;

[BotCallbackQuery("skip_tags")]
[RequireState<TrackLinkStates>(TrackLinkStates.WaitingForTags)]
public class SkipTags(LinkAdditionHandler handler) : ICallbackQuery
{
    public async Task HandleAsync(CallbackQuery callbackQuery, CancellationToken ct)
    {
        await handler.HandleAddLink([], ct);
    }
}
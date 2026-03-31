using LinkTracker.Bot.Core.HandlerTypes;
using LinkTracker.Bot.Core.HandlerTypes.Attributes;
using LinkTracker.Bot.Core.StateMachine.Attributes;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace LinkTracker.Bot.App.Features.Bot.TrackLink.Flow;

[UpdateHandler(UpdateType.Message)]
[RequireState<TrackLinkStates>(TrackLinkStates.WaitingForTags)]
public class ReceiveTags(LinkAdditionHandler handler) : IUpdateHandler
{
    public async Task HandleAsync(Update update, CancellationToken ct)
    {
        var tags = ParseTags(update.Message!.Text!);
        await handler.HandleAddLink(tags, ct);
    }

    private List<string> ParseTags(string message)
    {
        return message.Split(", ", StringSplitOptions.TrimEntries).ToList();
    }
}
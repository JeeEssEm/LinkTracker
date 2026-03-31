using LinkTracker.Bot.Core.StateMachine;
using LinkTracker.Bot.Core.StateMachine.Abstractions;

namespace LinkTracker.Bot.Core.Middlewares;

public class StateMiddleware(UpdateDelegate next)
{
    public async Task InvokeAsync(UpdateContext ctx, UserSession userSession, IStateController stateController)
    {
        var userState = await stateController.GetStateAsync(userSession.ChatId);
        userSession.SetState(userState);

        await next(ctx);

        if (userSession.State != userState)
        {
            await stateController.SetStateAsync(userSession.ChatId, userSession.State);
        }
    }
}

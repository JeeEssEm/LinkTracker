using LinkTracker.Bot.Core.StateMachine;

namespace LinkTracker.Bot.Core.Middlewares;

internal delegate Task InternalUpdateDelegate(UpdateContext context, IServiceProvider sp);

public delegate Task UpdateDelegate(UpdateContext context);

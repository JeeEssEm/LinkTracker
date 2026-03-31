using System.Collections.Concurrent;
using LinkTracker.Bot.Core.StateMachine.Abstractions;

namespace LinkTracker.Bot.App.StateController;

public class InMemoryStateController : IStateController
{
    private readonly ConcurrentDictionary<long, string?> _userStates = new();

    public Task SetStateAsync(long chatId, string? state)
    {
        _userStates[chatId] = state;
        return Task.CompletedTask;
    }

    public Task<string?> GetStateAsync(long chatId) => Task.FromResult(_userStates.GetValueOrDefault(chatId));
}
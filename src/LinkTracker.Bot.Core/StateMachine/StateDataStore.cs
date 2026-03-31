using System.Collections.Concurrent;
using LinkTracker.Bot.Core.StateMachine.Abstractions;

namespace LinkTracker.Bot.Core.StateMachine;

public class StateDataStore : IStateDataStore
{
    private readonly ConcurrentDictionary<long, object> _store = new();

    public void SetData<T>(long userId, T data)
    {
        if (data != null)
        {
            _store[userId] = data;
        }
    }

    public T GetData<T>(long userId)
    {
        if (_store.TryGetValue(userId, out var data))
        {
            return (T)data;
        }

        throw new ArgumentException("data not found");
    }

    public void UpdateData<T>(long userId, Func<T, T> predicate)
    {
        if (_store.TryGetValue(userId, out var data))
        {
            _store[userId] = predicate.Invoke((T)data);
        }
    }

    public void ResetData(long userId)
    {
        _store.Remove(userId, out _);
    }
}
namespace LinkTracker.Bot.Core.StateMachine.Abstractions;

public interface IStateDataStore
{
    void SetData<T>(long userId, T data);
    T GetData<T>(long userId);
    void UpdateData<T>(long userId, Func<T, T> predicate);
    void ResetData(long userId);
}

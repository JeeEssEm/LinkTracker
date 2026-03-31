namespace LinkTracker.Bot.Core.StateMachine;

public class UserSession
{
    public long ChatId { get; internal set; }
    public string? State { get; private set; }

    public void SetState<T>(T state) where T : Enum
    {
        State = StateHelpers.GetStateCode(state);
    }

    public bool IsInState<T>(T state) where T : Enum
    {
        return StateHelpers.GetStateCode(state) == State;
    }

    internal void SetState(string? state)
    {
        State = state;
    }

    public void ResetState()
    {
        State = null;
    }
}
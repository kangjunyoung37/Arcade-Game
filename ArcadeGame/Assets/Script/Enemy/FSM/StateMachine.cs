using System;

public sealed class StateMachine
{
    public IState CurrentState { get; private set; }
    public IState PreviousState { get; private set; }

    public void Start(IState initialState)
    {
        if (initialState == null)
        {
            throw new ArgumentNullException(nameof(initialState));
        }
        if(CurrentState != null)
        {
            throw new InvalidOperationException("State machine이 이미 시작되었습니다.");
        }

        CurrentState = initialState;
        CurrentState.Enter();
    }

   public void ChangeState(IState nextState)
    {
        if (nextState == null)
        {
            throw new ArgumentNullException(nameof(nextState));
        }

        if (ReferenceEquals(CurrentState, nextState))
        {
            return;
        }

        CurrentState?.Exit();

        PreviousState = CurrentState;
        CurrentState = nextState;

        CurrentState.Enter();
    }

    public void Tick(float deltaTime)
    {
        CurrentState?.Tick(deltaTime);
    }

    public void Stop()
    {
        CurrentState?.Exit();

        PreviousState = CurrentState;
        CurrentState = null;
    }

        public bool IsInState(IState state)
    {
        return ReferenceEquals(CurrentState, state);
    }
}

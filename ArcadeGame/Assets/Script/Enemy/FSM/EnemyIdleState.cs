using UnityEngine;

public sealed class EnemyIdleState : IState
{
    private readonly EnemyController _owner;

    private float _remainingIdleTime;
    private float _remainingDetectionTime;

    public EnemyIdleState(EnemyController owner)
    {
        _owner = owner;
    }

    public void Enter()
    {
        _owner.StopMoving();

        _remainingIdleTime = Random.Range(
            _owner.MinIdleDuration,
            _owner.MaxIdleDuration);
    }

    public void Tick(float deltaTime)
    {
      _remainingIdleTime -= deltaTime;

        if (_remainingIdleTime > 0f)
        {
            return;
        }

        IState patrolState = _owner.PatrolState;

        if (patrolState != null)
        {
            _owner.ChangeState(patrolState);
        }
    }

    public void Exit()
    {
    }
}

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

        // Idle 진입 직후 바로 탐지
        _remainingDetectionTime = 0f;
    }

    public void Tick(float deltaTime)
    {
        _remainingIdleTime -= deltaTime;
        _remainingDetectionTime -= deltaTime;

        if (TryDetectPlayer())
        {
            return;
        }
        TryStartPatrol();
    }

    private bool TryDetectPlayer()
    {
        if(_remainingDetectionTime > 0f)
        {
            return false;
        }
        _remainingDetectionTime = _owner.DetectionInterval;

        if(!_owner.CanDetectPlayer())
        {
            return false;
        }
        IState alertState = _owner.AlertState;
        if (alertState != null)
        {
            return false;
        }
        _owner.ChangeState(alertState);
        return true;
    }

    private void TryStartPatrol()
    {
        if (_remainingIdleTime > 0f)
        {
            return;
        }

        IState patrolState = _owner.PatrolState;

        if (patrolState == null)
        {
            return;
        }
        _owner.ChangeState(patrolState);
    }
    public void Exit()
    {
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyPatrolState : IState
{
    private readonly EnemyController _owner;

    private float _remainingDetectionTime;
    private bool _hasDestination;

    public EnemyPatrolState(EnemyController owner)
    {
        _owner = owner;
    }
    public void Enter()
    {
        _remainingDetectionTime = 0f;
        _hasDestination = _owner.TryGetNextPatrolDestination(out Vector3 destination);
        if(!_hasDestination)
        {
            return;    
        }
        _hasDestination = _owner.TryMoveTo(destination, _owner.PatrolStoppingDistance);
    }
    
    public void Tick(float deltaTime)
    {
        if(TryDetectPlayer(deltaTime))
        {
            return;
        }
        if(!_hasDestination || _owner.HasPathFailed() || _owner.HasReachedDestination())
        {
            _owner.ChangeState(_owner.IdleState);
        }
    }
    public void Exit()
    {
        _owner.StopMoving();
    }
    private bool TryDetectPlayer(float deltaTime)
    {
        _remainingDetectionTime -= deltaTime;
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
        if(alertState == null)
        {
            return false;
        }
        _owner.ChangeState(alertState);
        return true;
    }

} 

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyPatrolState : IState
{
    private readonly EnemyController _owner;

    private bool _hasDestination;

    public EnemyPatrolState(EnemyController owner)
    {
        _owner = owner;
    }
    public void Enter()
    {
        _hasDestination = _owner.TryGetNextPatrolDestination(out Vector3 destination);
        if(!_hasDestination)
        {
            return;    
        }
        _hasDestination = _owner.TryMoveTo(destination, _owner.PatrolStoppingDistance);
    }
    
    public void Tick(float deltaTime)
    {
        if(!_hasDestination || _owner.HasPathFailed() || _owner.HasReachedDestination())
        {
            _owner.ChangeState(_owner.IdleState);
        }
    }
    public void Exit()
    {
        _owner.StopMoving();
    }

} 

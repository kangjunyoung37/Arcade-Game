using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class EnemyChaseState : IState
{
    private readonly EnemyController _owner;

    private float _remainingPathUpdateTime;
    private bool _hasMoveRequest;

    public EnemyChaseState(EnemyController owner)
    {
        _owner = owner;
    }
    public void Enter()
    {
        _remainingPathUpdateTime = 0f;
        _hasMoveRequest = false;
    }
    public void Tick(float deltaTime)
    {
        if(!_owner.HasTarget || !_owner.CanNaviagate)
        {
            _owner.ChangeState(_owner.IdleState);
            return;
        }
        if (_owner.IsTargetInAttackRange())
        {
            StopRequestedMovement();
            _owner.FaceLastKnownPosition(deltaTime);
            IState attackState = _owner.AttackState;
            if(attackState != null)
            {
                _owner.ChangeState(attackState);
            }
            return;
        }
        if(!_owner.CanSeePlayer && Time.time - _owner.LastStimulusTime >= _owner.ChaseMemoryDuration)
        {
            ChangeToSearch();
            return;
        }
        _remainingPathUpdateTime -= deltaTime;
        if (_hasMoveRequest)
        {
            if(_owner.IsPathPendinng)
            {
                return;
            }
            if (_owner.HasPathFailed())
            {
                ChangeToSearch();
                return;
            }
            if(!_owner.CanSeePlayer && _owner.HasReachedDestination())
            {
                ChangeToSearch();
                return;
            }
        }
        if(_hasMoveRequest && _remainingPathUpdateTime > 0f)
        {
            return;
        }
        _remainingPathUpdateTime = _owner.ChasePathUpdateInterval;
        bool accepted = _owner.TryMoveTo(_owner.LastKnownPlayerPosition, _owner.ChaseStoppingDistance);

        if(!accepted)
        {
            ChangeToSearch();
            return;
        }
        _hasMoveRequest = true;
    }

    public void Exit()
    {
        _owner.StopMoving();
        _hasMoveRequest = false;
    }

    private void StopRequestedMovement()
    {
        if(_hasMoveRequest)
        {
            return;
        }

        _owner.StopMoving();
        _hasMoveRequest = false;
        _remainingPathUpdateTime = 0f;
    }

    private void ChangeToSearch()
    {
        IState nextState = _owner.SearchState ?? _owner.IdleState;
        _owner.ChangeState(nextState);
    }

}

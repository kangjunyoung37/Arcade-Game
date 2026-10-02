using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class EnemySearchState : IState
{
    private enum SearchPhase
    {
        MovingToLastKnownPosition,
        LookingAround,
    }
    private readonly EnemyController _owner;
    private SearchPhase _phase;

    private float _remainingMoveTime;
    private float _remainingLookTime;
    private float _remainingChaseRetryTime;
    
    private float _stimulusTimeOnEnter;
    private bool _previousAgentUpdateRotation;

    public EnemySearchState(EnemyController owner)
    {
        _owner = owner != null ? owner : throw new ArgumentNullException(nameof(owner));
    }
    public void Enter()
    {
        _previousAgentUpdateRotation = _owner.AgentUpdatesRotation;
        _stimulusTimeOnEnter = _owner.LastStimulusTime;

        _remainingMoveTime = _owner.SearchMoveTimeout;
        _remainingLookTime = _owner.SearchLookDuration;
        _remainingChaseRetryTime = _owner.SearchChaseRetryDelay;

        _phase = SearchPhase.MovingToLastKnownPosition;

        Vector3 destination = _owner.LastKnownPlayerPosition;
        bool accepted = _owner.TryMoveTo(destination, _owner.SearchStoppingDistance);
        if (!accepted)
        {
            BeginLookingAround();
        }
    }
    public void Tick(float deltaTime)
    {
        if (!_owner.HasTarget)
        {
            _owner.ChangeState(_owner.IdleState);
            return;
        }
        _remainingChaseRetryTime -= deltaTime;

        if (_owner.CanSeePlayer)
        {
            if(_remainingChaseRetryTime <= 0f)
            {
                _owner.ChangeState(_owner.ChaseState);
                return;
            }
        }
        else if(_owner.LastStimulusTime > _stimulusTimeOnEnter)
        {
            _owner.ChangeState(_owner.AlertState);
            return;
        }
        switch (_phase)
        {
            case SearchPhase.MovingToLastKnownPosition:
                TickMovement(deltaTime);
                break;
            
            case SearchPhase.LookingAround:
                TickLookAround(deltaTime);
                break;  
        }
    }
    public void Exit()
    {
        _owner.StopMoving();
        _owner.AgentUpdatesRotation = _previousAgentUpdateRotation;
    }

    private void TickMovement(float deltaTime)
    {
        _remainingMoveTime -= deltaTime;
        if(!_owner.CanNaviagate || _remainingMoveTime <= 0f)
        {
            BeginLookingAround();
            return;
        }
        if (_owner.IsPathPendinng)
        {
            return;
        }
        if(_owner.HasPathFailed() || _owner.HasReachedDestination())
        {
            BeginLookingAround();
        }
    }

    private void BeginLookingAround()
    {
        _owner.StopMoving();
        _owner.AgentUpdatesRotation = false;
        _phase = SearchPhase.LookingAround; 
        _remainingLookTime = _owner.SearchLookDuration;
    }
    private void TickLookAround(float deltaTime)
    {
        _remainingLookTime -= deltaTime;
        if(_remainingLookTime <= 0f)
        {
            _owner.ChangeState(_owner.IdleState);
            return;
        }
        float rotationAmount = _owner.SearchTurnSpeed * deltaTime;
        if(rotationAmount > 0f)
        {
            _owner.transform.Rotate(Vector3.up, rotationAmount,Space.World);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class EnemyAlertState : IState
{
    private readonly EnemyController _owner;
    private float _remainingReactionTime;

    public EnemyAlertState(EnemyController owner)
    {
        _owner = owner;
    }
    public void Enter()
    {
        _owner.StopMoving();
        _remainingReactionTime = _owner.AlertReactionTime;
    }

    public void Tick(float deltaTime)
    {
        if(deltaTime <= 0f)
            return;
        if (!_owner.HasTarget)
        {
            _owner.ChangeState(_owner.IdleState);
            return;
        }

        _owner.FaceLastKnownPosition(deltaTime);
        _remainingReactionTime -= deltaTime;

        if(_remainingReactionTime > 0f)
            return;
        float timeSinceStimulus = Time.time - _owner.LastStimulusTime;

        bool hasRecentStimulus = timeSinceStimulus < _owner.AlertMemoryDuration;
        
        if (_owner.CanSeePlayer || hasRecentStimulus)
        {
            IState nextState = _owner.ChaseState ?? _owner.SearchState ?? _owner.IdleState;
            _owner.ChangeState(nextState);

            return;
        }
        _owner.ChangeState(_owner.SearchState ?? _owner.IdleState);
    }
    public void Exit()
    {
    }
    
}

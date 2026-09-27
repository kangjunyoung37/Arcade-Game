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
        _owner.FaceLastKnownPosition(deltaTime);
        _remainingReactionTime -= deltaTime;

        if(_remainingReactionTime > 0f)
        {
            return;
        }
        if (_owner.CanSeePlayer)
        {
            IState chaseState_ = _owner.ChaseState;
            if(chaseState_ != null)
            {
                _owner.ChangeState(chaseState_);
            }
            //Chase 구현
            return;
        }
        float timeSicneStimulus = Time.time - _owner.LastStimulusTime;
        if(timeSicneStimulus < _owner.AlertMemoryDuration)
        {
            return;
        }
        IState nextState = _owner.SearchState ?? _owner.IdleState;
        _owner.ChangeState(nextState);
    }
    public void Exit()
    {
    }
    
}

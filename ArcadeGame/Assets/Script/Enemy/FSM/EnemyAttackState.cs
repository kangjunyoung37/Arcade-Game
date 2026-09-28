using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class EnemyAttackState : IState
{
    private readonly EnemyController _owner;

    private float _nextAttackTime;

    public EnemyAttackState(EnemyController owner)
    {
        _owner = owner != null ? owner : throw new ArgumentException(nameof(owner));
    }

    public void Enter()
    {
        _owner.StopMoving();
    }
    public void Tick(float deltaTime)
    {
        if (!_owner.HasTarget)
        {
            _owner.ChangeState(_owner.IdleState);
            return;
        }
        if (!_owner.CanSeePlayer)
        {
            _owner.ChangeState(_owner.ChaseState);
            return;
        }
        if (!_owner.IsTargetWithinAttackHoldRange())
        {
            _owner.ChangeState(_owner.ChaseState);
            return;
        }
        _owner.FaceLastKnownPosition(deltaTime);

        if(Time.time < _nextAttackTime)
        {
            return;
        }

        if (_owner.TryRequestAttack())
        {
            _nextAttackTime = Time.time + _owner.AttackInterval;
        }
    }
    public void Exit()
    {
        
    }

}

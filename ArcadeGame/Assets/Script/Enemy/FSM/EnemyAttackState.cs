using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class EnemyAttackState : IState
{
    private readonly EnemyController _owner;

    private int _shotsInBurst;

    private float _nextAttackTime;

    public EnemyAttackState(EnemyController owner)
    {
        _owner = owner != null ? owner : throw new ArgumentException(nameof(owner));
    }

    public void Enter()
    {
        _owner.StopMoving();
        _shotsInBurst = 0;
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

        if (!_owner.TryRequestAttack())
        {
            return;
        }
        RecordShotRequest();
    }
    public void Exit()
    {
        if(_shotsInBurst > 0)
        {
            _nextAttackTime = Mathf.Max(_nextAttackTime, Time.time + _owner.BurstCooldown);
        }
        _shotsInBurst = 0;
    }
    private void RecordShotRequest()
    {
        _shotsInBurst++;
        if (_shotsInBurst >= _owner.BurstCount)
        {
            _shotsInBurst = 0;
            _nextAttackTime = Time.time + _owner.BurstCooldown;
            return;
        }
        _nextAttackTime = Time.time + _owner.AttackInterval;
    }

}

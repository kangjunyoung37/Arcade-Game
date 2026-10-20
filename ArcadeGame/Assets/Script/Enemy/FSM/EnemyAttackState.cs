using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class EnemyAttackState : IState
{
    private readonly EnemyController _owner;

    public EnemyAttackState(EnemyController owner)
    {
        _owner = owner != null ? owner : throw new ArgumentException(nameof(owner));
    }

    public void Enter()
    {
        _owner.StopMoving();
        if(_owner.CombatDecision != null)
            _owner.CombatDecision.ResetDecision();

        if(_owner.StrafeMovement != null)
            _owner.StrafeMovement.Begin();
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
        EnemyCombatMoveIntent intent = EnemyCombatMoveIntent.Strafe;
        if(_owner.CombatDecision != null && _owner.CombatDecision.isActiveAndEnabled)
            intent = _owner.CombatDecision.Evaluate();
        if(_owner.StrafeMovement != null) 
            _owner.StrafeMovement.Tick(deltaTime, intent);

        _owner.FaceLastKnownPosition(deltaTime);

        //발사 가능 시점은 Controller가 공통으로 판단
        _owner.TryRequestAttack();
    }
    public void Exit()
    {
        if(_owner.StrafeMovement != null)
             _owner.StrafeMovement.End();
    }

}

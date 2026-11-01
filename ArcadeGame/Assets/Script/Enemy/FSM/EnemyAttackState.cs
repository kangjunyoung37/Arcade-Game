using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class EnemyAttackState : IState
{
    private readonly EnemyController _owner;
    private bool _hasAimSnapshot;
    private Vector3 _aimSnapshot;

    private bool _suppressing;
    private float _suppressionEndTime;
    private float _nextSuppressionDecisionTime;
    private int _suppressionShots;
    public EnemyAttackState(EnemyController owner)
    {
        _owner = owner != null ? owner : throw new ArgumentException(nameof(owner));
    }

    public void Enter()
    {
        _suppressing = false;
        _suppressionShots = 0;
        _hasAimSnapshot = false;

        _owner.StopMoving();

        RememberVisibleAim();
        BeginCombatMovement();
    }
    public void Tick(float deltaTime)
    {
        if (deltaTime <= 0f)
            return;

        if (!_owner.HasTarget)
        {
            _owner.ChangeState(_owner.IdleState);
            return;
        }

        if (!_owner.CanSeePlayer)
        {
            TickLostSight(deltaTime);
            return;
        }

        // 보이는 동안만 목격 위치를 갱신합니다.
        RememberVisibleAim();

        if (!_owner.IsTargetWithinAttackHoldRange())
        {
            _owner.ChangeState(_owner.ChaseState);
            return;
        }

        // 위협사격 중 다시 발견했다면 일반 전투로 복귀합니다.
        if (_suppressing)
        {
            _suppressing = false;
            BeginCombatMovement();
        }

        EnemyCombatMoveIntent intent = EnemyCombatMoveIntent.Hold;

        if (_owner.CombatDecision != null && _owner.CombatDecision.isActiveAndEnabled)
            intent = _owner.CombatDecision.Evaluate(deltaTime);
        
        if (_owner.StrafeMovement != null)
            _owner.StrafeMovement.Tick(deltaTime, intent);

        _owner.FaceLastKnownPosition(deltaTime);
        _owner.TryRequestAttack();
    }
    public void Exit()
    {
        if (_owner.StrafeMovement != null)
            _owner.StrafeMovement.End();

        _suppressing = false;
        _hasAimSnapshot = false;
        _suppressionShots = 0;

    }
    private void RememberVisibleAim()
    {
        if (!_owner.CanSeePlayer)
            return;

        _aimSnapshot = _owner.LastKnownAimPosition;
        _hasAimSnapshot = true;
    }

    private void BeginCombatMovement()
    {
        if (_owner.CombatDecision != null)
            _owner.CombatDecision.ResetDecision();

        if (_owner.StrafeMovement != null)
            _owner.StrafeMovement.Begin();
    }

    private void TickLostSight(float deltaTime)
    {
        // 이번 시야 상실에서 시작할 수 없다면 바로 추적합니다.
        if (!_suppressing && !TryBeginSuppression())
        {
            _owner.ChangeState(_owner.ChaseState);
            return;
        }

        if (!_owner.CanReactToHit)
        {
            _owner.ChangeState(_owner.IdleState);
            return;
        }

        bool finished = !_owner.CanUseSuppression || _owner.WeaponIsReloading || Time.time >= _suppressionEndTime || _suppressionShots >= _owner.MaxSuppressionShots;

        if (finished)
        {
            _owner.ChangeState(_owner.ChaseState);
            return;
        }

        _owner.FacePosition(_aimSnapshot, deltaTime);

        // 실제 발사에 성공했을 때만 횟수를 셉니다.
        if (_owner.TryRequestSuppressionFire(_aimSnapshot))
        {
            _suppressionShots++;

            if (_suppressionShots >= _owner.MaxSuppressionShots)
                _owner.ChangeState(_owner.ChaseState);
        }
    }

    private bool TryBeginSuppression()
    {
        if (!_owner.HasCompletedVisualReaction)
            return false;
            
        if (!_owner.CanUseSuppression || !_owner.CanReactToHit || !_owner.HasUsableWeapon ||_owner.WeaponIsReloading ||!_hasAimSnapshot)
            return false;
        
        float now = Time.time;

        if (now < _nextSuppressionDecisionTime)
            return false;

        float chance = _owner.SuppressionChance;

        if (chance <= 0f)
            return false;

        // 확률에서 탈락해도 즉시 재추첨하지 않습니다.
        _nextSuppressionDecisionTime = now + _owner.SuppressionCooldown;

        if (chance < 1f && UnityEngine.Random.value >= chance) 
            return false;

        if (_owner.StrafeMovement != null)
            _owner.StrafeMovement.End();

        _owner.StopMoving();

        _suppressing = true;
        _suppressionShots = 0;
        _suppressionEndTime = now + _owner.SuppressionDuration;

        return true;
    }
}


using System;
using UnityEngine;
using UnityEngine.AI;

public sealed class EnemyDodgeState : IState
{
    private const float ArrivalTolerance = 0.05f;

    private readonly EnemyController _owner;
    private readonly NavMeshAgent _agent;
    private readonly EnemyDodgeSensor _sensor;

    private Vector3 _destination;
    private float _remainingTime;
    private float _moveSpeed;

    private bool _prepared;
    private bool _entered;
    private bool _failed;
    private bool _settingsSaved;
    private bool _previousUpdateRotation;

    private bool CanNavigate => _agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh && _agent.updatePosition && !_agent.isOnOffMeshLink;

    private Vector3 GroundPosition => _agent.nextPosition - Vector3.up * _agent.baseOffset;

    public EnemyDodgeState( EnemyController owner, NavMeshAgent agent, EnemyDodgeSensor sensor)
    {
        _owner = owner != null ? owner : throw new ArgumentNullException(nameof(owner));
        _agent = agent != null ? agent : throw new ArgumentNullException(nameof(agent));
        _sensor = sensor;
    }

    public bool TryPrepare()
    {
        if (_entered || _prepared || !CanNavigate || _sensor == null || !_sensor.isActiveAndEnabled)
            return false;
        
        if (!_sensor.TryFindDodgePosition(out _destination))
            return false;

        _prepared = true;
        return true;
    }

    public void Enter()
    {
        _entered = true;
        _settingsSaved = false;

        _failed = !_prepared || !CanNavigate || _sensor == null || !_sensor.isActiveAndEnabled;
        _prepared = false;

        // Enter 안에서 즉시 다른 상태로 전환하지 않음
        if (_failed)
            return;

        // 이전 상태의 Exit가 끝난 뒤 설정을 저장
        _previousUpdateRotation = _agent.updateRotation;
        _settingsSaved = true;

        _owner.StopMoving();
        _owner.PauseAttackCycle();

        // 구르기 동안 현재 바라보는 방향을 유지.
        _agent.updateRotation = false;
        _remainingTime = _owner.DodgeDuration;

        _moveSpeed = Vector3.Distance(GroundPosition, _destination) / _remainingTime;
    }

    public void Tick(float deltaTime)
    {
        if (deltaTime <= 0f)
            return;

        if (_failed || !CanNavigate || !_owner.CanReactToHit || !_owner.HasTarget || _sensor == null || !_sensor.isActiveAndEnabled)
        {
            Finish();
            return;
        }

        Vector3 origin = GroundPosition;

        bool arrived = (_destination - origin).sqrMagnitude <= ArrivalTolerance * ArrivalTolerance;

        if (arrived || _remainingTime <= 0f)
        {
            Finish();
            return;
        }

        // 프레임이 길어져도 남은 동작 시간 이상 이동하지 않음.
        float stepTime = Mathf.Min(deltaTime, _remainingTime);

        Vector3 nextPosition = Vector3.MoveTowards( origin, _destination, _moveSpeed * stepTime);

        if (!_sensor.CanTraverseDodgeSegment(origin, nextPosition))
        {
            Finish();
            return;
        }

        // Transform을 직접 바꾸지 않고 Agent를 통해 이동.
        _agent.Move(nextPosition - origin);

        _remainingTime -= stepTime;
    }

    public void Exit()
    {
        if (_entered)
        {
            _owner.StopMoving();
            _owner.NotifyDodgeExited();
        }

        if (_settingsSaved && _agent != null)
            _agent.updateRotation = _previousUpdateRotation;

        _prepared = false;
        _entered = false;
        _failed = false;
        _settingsSaved = false;
    }

    private void Finish()
    {
        if (!CanNavigate || !_owner.CanReactToHit || !_owner.HasTarget)
        {
            _owner.ChangeState(_owner.IdleState);
            return;
        }

        if (_owner.CanSeePlayer)
        {
            IState nextState = _owner.IsTargetInAttackRange() ? _owner.AttackState : _owner.ChaseState;
            _owner.ChangeState(nextState);
            return;
        }

        _owner.ChangeState(_owner.SearchState ?? _owner.IdleState);
    }
}
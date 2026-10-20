using System;
using UnityEngine;
using UnityEngine.AI;

public sealed class EnemyCoverState : IState
{
    private enum Phase
    {
        Moving,
        Holding
    }

    private const float StoppingDistance = 0.1f;
    private const float ArrivalTolerance = 0.25f;
    private const float PointMovementTolerance = 0.05f;

    private readonly EnemyController _owner;
    private readonly NavMeshAgent _agent;
    private readonly EnemyCoverSensor _sensor;

    private CoverPoint _point;
    private Vector3 _destination;
    private Vector3 _originalPointPosition;

    private Phase _phase;
    private bool _entered;
    private bool _failed;
    private bool _settingsSaved;

    private float _remainingMoveTime;
    private float _remainingHoldTime;
    private float _remainingCheckTime;

    private float _previoudSpeed;
    private float _previousAcceleration;
    private float _previousStoppingDistance;
    private bool _previousAutoBraking;
    private bool _previousUpdateRotation;
    private bool _reloadOnArrival;
    private float _remainingReloadWait;

    private bool CanNavigate => _agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh;
    private Vector3 GroundPosition => _agent.nextPosition - Vector3.up * _agent.baseOffset;

    public bool IsHolding => _entered && _phase == Phase.Holding;
    
    public EnemyCoverState(EnemyController onwer, NavMeshAgent agent, EnemyCoverSensor sensor)
    {
        _owner = onwer != null ? onwer : throw new ArgumentNullException(nameof(onwer));
        _agent = agent != null ? agent : throw new ArgumentNullException(nameof(_agent));

        _sensor = sensor;
    }

    public bool TryPrepare(bool reloadOnArrival = false)
    {
        if(_entered || _point != null || !CanNavigate || _sensor == null || !_sensor.isActiveAndEnabled || !_owner.HasTarget)
            return false;
        if(!_sensor.TryFindCover(_owner.LastKnownPlayerPosition, out CoverPoint selectedPoint, out Vector3 destination))
            return false;
        if(selectedPoint == null || !selectedPoint.TryReserve(_owner))
            return false;
        _point = selectedPoint;
        _destination = destination;
        _originalPointPosition = selectedPoint.Position;

        _reloadOnArrival = reloadOnArrival;
        return true;
    }

    public void Enter()
    {
        _entered = true;
        _failed = false;
        _settingsSaved = false;
        _phase = Phase.Moving;

        _remainingMoveTime = _owner.CoverMoveTimeout;
        _remainingHoldTime = _owner.CoverHoldDuration;
        _remainingCheckTime = 0f;

        if(!CanNavigate || _point == null || !_point.isActiveAndEnabled || !_point.IsReservedBy(_owner))
        {
            _failed = true;
            return;
        }

        _previoudSpeed = _agent.speed;
        _previousAcceleration = _agent.acceleration;
        _previousStoppingDistance = _agent.stoppingDistance;
        _previousAutoBraking = _agent.autoBraking;
        _previousUpdateRotation = _agent.updateRotation;
        _settingsSaved = true;
        
        _owner.StopMoving();

        _agent.speed = _owner.CoverMoveSpeed;
        _agent.acceleration = _owner.CoverAcceleration;
        _agent.stoppingDistance = StoppingDistance;
        _agent.autoBraking = true;
        _agent.updateRotation = false;

        if (!_agent.SetDestination(_destination))
        {
            _failed = true;
            return;
        }
        _agent.isStopped = false;
    }
    
    public void Tick(float deltaTime)
    {
        if(deltaTime <= 0f)
            return;
        if(_failed || !CanNavigate || !_owner.HasTarget || _sensor == null || !_sensor.isActiveAndEnabled || _point == null || !_point.isActiveAndEnabled || !_point.IsReservedBy(_owner))
        {
            ReturnToNormalBehaviour();
            return;
        }

        if((_point.Position - _originalPointPosition).sqrMagnitude > PointMovementTolerance * PointMovementTolerance)
        {
            ReturnToNormalBehaviour();
            return;
        }

        _remainingCheckTime -= deltaTime;
        if(_remainingCheckTime <= 0f)
        {
            _remainingCheckTime = _owner.CoverCheckInterval;;

            Vector3 positionToCheck = _phase == Phase.Moving ? _destination : GroundPosition;
            if(!_sensor.IsCoverPositionSafe(positionToCheck, _owner.LastKnownPlayerPosition))
            {
                ReturnToNormalBehaviour();
                return;
            }
        }

        if(_phase == Phase.Moving)
        {
            TickMoving(deltaTime);
            if(!_entered || _phase != Phase.Moving)
                return;

            _owner.FaceLastKnownPosition(deltaTime);

            if(!_owner.WeaponIsReloading)
                _owner.TryRequestAttack();
            return;
        }

        //마지막으로 본 위치 보기
        TickHolding(deltaTime);
    }

    private void TickMoving(float deltaTime)
    {
        _remainingMoveTime -= deltaTime;

        if(_remainingMoveTime <= 0f)
        {
            ReturnToNormalBehaviour();
            return;
        }

        if(_agent.pathPending)
            return;
        bool closeEnough = (GroundPosition - _destination).sqrMagnitude <= ArrivalTolerance * ArrivalTolerance;

        bool arrived = closeEnough && (!_agent.hasPath || (_agent.pathStatus == NavMeshPathStatus.PathComplete && _agent.remainingDistance <= StoppingDistance));

        if (arrived)
        {
            //실제가 간 위치도 엄폐 조건을 만족해야함
            if(!_sensor.IsCoverPositionSafe(GroundPosition, _owner.LastKnownPlayerPosition))
            {
                ReturnToNormalBehaviour();
                return;
            }

            _owner.StopMoving();
            _owner.PauseAttackCycle();
            _agent.updateRotation = false;

            _phase = Phase.Holding;
            _remainingHoldTime = _owner.CoverHoldDuration;
            _remainingReloadWait = _reloadOnArrival ? _owner.CoverReloadWaitLimit : 0f;
            return;
        }

        if(!_agent.hasPath || _agent.pathStatus != NavMeshPathStatus.PathComplete)
        {
            ReturnToNormalBehaviour();
        }
    }

    private void TickHolding(float deltaTime)
    {
        _owner.FaceLastKnownPosition(deltaTime);

        if(!_reloadOnArrival)
        {
            _remainingHoldTime -= deltaTime;
            if(_remainingHoldTime <= 0f)
                ReturnToNormalBehaviour();
            return;
        }

        if (!_owner.HasUsableWeapon)
        {
            ReturnToNormalBehaviour();
            return;
        }

        bool reloadComplete = !_owner.WeaponIsReloading && !_owner.WeaponNeedsReload;

        if(reloadComplete)
        {
            _reloadOnArrival = false;
            _remainingHoldTime = _owner.PostReloadHoldDuration;
            return;
        }
        _remainingReloadWait -= deltaTime;
        if(_remainingReloadWait <= 0f)
        {
            //재장전이 시작되지 않거나 진행이 멈춤
            ReturnToNormalBehaviour();
            return;
        }
        if (!_owner.WeaponIsReloading)
        {
            //볼트/펌프 작동 중이면 실패할 수 있음
            //다음 Tick에서 다시 확인 위 제한시간까지 기다림
            _owner.TryStartCoverReload();
        }
    }

    public void Exit()
    {
        if(_entered)
            _owner.NotifyCoverExited();

        if(_point != null)
            _point.Release(_owner);
        
        _point = null;

        if(_entered && _owner != null)
            _owner.StopMoving();
        if(_settingsSaved && _agent != null)
        {
            _agent.speed = _previoudSpeed;
            _agent.acceleration = _previousAcceleration;
            _agent.stoppingDistance = _previousStoppingDistance;
            _agent.autoBraking = _previousAutoBraking;
            _agent.updateRotation = _previousUpdateRotation;
        }
        _settingsSaved = false;
        _entered = false;
        _failed = false;
        _reloadOnArrival = false;
        _remainingReloadWait = 0f;
    }

    private void ReturnToNormalBehaviour()
    {
        if(!_owner.HasTarget || !CanNavigate)
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

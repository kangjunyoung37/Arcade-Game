using System;
using UnityEngine;
using UnityEngine.AI;

public sealed class EnemyCoverState : IState
{
    private enum Phase
    {
        Moving,         // 처음 엄폐로 이동
        Holding,        // 엄폐 대기,재장전
        MovingToPeek,   // 피킹 위치로 이동
        Firing,         // 피킹 사격
        Returning       // 원래 엄폐로 복귀
    }

    private const float StoppingDistance = 0.1f;
    private const float ArrivalTolerance = 0.25f;
    private const float PointMovementTolerance = 0.05f;
    private const float InitialLeftPeekChance = 0.5f;
    private readonly EnemyController _owner;
    private readonly NavMeshAgent _agent;
    private readonly EnemyCoverSensor _sensor;

    private CoverPoint _point;
    private Vector3 _destination;
    private Vector3 _peekDestination;
    private Vector3 _originalPointPosition;

    private Phase _phase;
    private float _remainingMoveTime;
    private float _remainingHoldTime;
    private float _remainingCheckTime;
    private float _previoudSpeed;
    private float _previousAcceleration;
    private float _previousStoppingDistance;
    private float _remainingReloadWait;
    private float _remainingPeekTime;

    private bool _previousAutoBraking;
    private bool _previousUpdateRotation;
    private bool _reloadOnArrival;
    private bool _entered;
    private bool _failed;
    private bool _settingsSaved;
    private bool _hasPreviousPeekSide;
    private int _shotsthisPeek;
    private int _peekCycles;
    private EnemyCoverSensor.PeekSide _previousPeekSide;
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
        _peekCycles = 0;
        _hasPreviousPeekSide = false;
        _shotsthisPeek = 0;
        _remainingPeekTime = 0f;

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
        if(_owner.TryReactToCloseFootstepFromCover())
            return;
        if((_point.Position - _originalPointPosition).sqrMagnitude > PointMovementTolerance * PointMovementTolerance)
        {
            ReturnToNormalBehaviour();
            return;
        }

        _remainingCheckTime -= deltaTime;
        if(_remainingCheckTime <= 0f)
        {
            _remainingCheckTime = _owner.CoverCheckInterval;;

            Vector3 positionToCheck = _phase == Phase.Holding ? GroundPosition : _destination;
            if(!_sensor.IsCoverPositionSafe(positionToCheck, _owner.LastKnownPlayerPosition))
            {
                ReturnToNormalBehaviour();
                return;
            }
        }

        switch (_phase)
        {
            case Phase.Moving:
            case Phase.MovingToPeek:
            case Phase.Returning:
                _owner.FaceLastKnownPosition(deltaTime);
                TickMoving(deltaTime);
                //최초 엄폐 진입 중의 이동사격만 유지
                //피킹 이동 중에는 쏘지 않고 도착 후 사격
                if(_entered && _phase == Phase.Moving && !_owner.WeaponIsReloading)
                {
                    _owner.TryRequestAttack();
                }
                break;
            case Phase.Holding:
                TickHolding(deltaTime);
                break;
            case Phase.Firing:
                TickPeekFiring(deltaTime);
                break;

        }
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
        Vector3 moveDestination = _phase == Phase.MovingToPeek ? _peekDestination : _destination;

        bool closeEnough = (GroundPosition - moveDestination).sqrMagnitude <= ArrivalTolerance * ArrivalTolerance;

        bool arrived = closeEnough && (!_agent.hasPath || (_agent.pathStatus == NavMeshPathStatus.PathComplete && _agent.remainingDistance <= StoppingDistance));

        if (arrived)
        {
            if(_phase == Phase.MovingToPeek)
            {
                _owner.StopMoving();
                _phase = Phase.Firing;
                _shotsthisPeek = 0;
                _remainingPeekTime = _owner.PeekExposureDuration;
                return;
            }
            BeginHolding();
            return;
        }

        if(!_agent.hasPath || _agent.pathStatus != NavMeshPathStatus.PathComplete)
        {
            ReturnToNormalBehaviour();
        }
    }

    private void HandleMovmeentFailure()
    {
        if(_phase == Phase.MovingToPeek)
        {
            BeginReturnToCover();
            return;
        }
        ReturnToNormalBehaviour();
    }

    private void BeginHolding()
    {
        if(!_sensor.IsCoverPositionSafe(GroundPosition, _owner.LastKnownPlayerPosition))
        {
            ReturnToNormalBehaviour();
            return;
        }
        _owner.StopMoving();
        _owner.PauseAttackCycle();

        _phase = Phase.Holding;
        _remainingHoldTime = _owner.SampleCoverHoldDuration();
        //최초 진입 목적과 피킹 후 탄약 상태를 함께 반영한다
        _reloadOnArrival = _reloadOnArrival || _owner.ShouldReloadInCover;
        _remainingReloadWait = _reloadOnArrival ? _owner.CoverReloadWaitLimit : 0f;
    }

    private bool BeginMove(Phase nextPhase, Vector3 destination, float timeout)
    {
        _owner.StopMoving();
        if(!_agent.SetDestination(destination))
            return false;
        _phase = nextPhase;
        _remainingMoveTime = timeout;
        _agent.isStopped = false;
        
        return true;
    }
    private void TickHolding(float deltaTime)
    {
        _owner.FaceLastKnownPosition(deltaTime);

        if(!_reloadOnArrival)
        {
            _remainingHoldTime -= deltaTime;
            if(_remainingHoldTime <= 0f)
                BeginPeekOrFinish();
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

    private void BeginPeekOrFinish()
    {
        bool memoryExpired = !_owner.CanSeePlayer && Time.time - _owner.LastStimulusTime > _owner.PeekMemoryDuration;
        if(_peekCycles >= _owner.MaxPeekCycles || memoryExpired)
        {
            ReturnToNormalBehaviour();
            return;
        }
        if (_owner.ShouldReloadInCover)
        {
            _reloadOnArrival = true;
            _remainingReloadWait = _owner.CoverReloadWaitLimit;
            return;
        }
        EnemyCoverSensor.PeekSide preferredSide = SelectPreferredPeekSide();

        if(!_sensor.TryFindPeekPosition(_point, _owner.LastKnownPlayerPosition, _owner.LastKnownAimPosition, out EnemyCoverSensor.PeekSide selectedSide, out Vector3 peekPosition, preferredSide))
        {
            ReturnToNormalBehaviour();
            return;
        }

        _peekDestination = peekPosition;
        if(!BeginMove(Phase.MovingToPeek, _peekDestination, _owner.PeekmoveTimeout))
        {
            ReturnToNormalBehaviour();
            return;
        }
        // 선호한 방향이 아니라 실제 이동을 시작한 방향을 기억
        _previousPeekSide = selectedSide;
        _hasPreviousPeekSide = true;
        _peekCycles++;
    }

    private void TickPeekFiring(float deltaTime)
    {
        _owner.FaceLastKnownPosition(deltaTime);
        _remainingPeekTime -= deltaTime;
        
        if(_remainingPeekTime <= 0f || _owner.WeaponIsReloading || !_owner.HasLoadedAmmunition)
        {
            BeginReturnToCover();
            return;
        }
        // 실제 시야, 조준, 공격 거리, 총구 장애물,
        // 무기별 발사 간격은 공통 사격 함수에서 검사한다.
        if(!_owner.TryRequestAttack())
            return;
        _shotsthisPeek++;
        if(_shotsthisPeek >= _owner.ShotsPerPeek)
            BeginReturnToCover();
        
    }

    private void BeginReturnToCover()
    {
        _owner.PauseAttackCycle();

        if(!_sensor.IsCoverPositionSafe(_destination, _owner.LastKnownPlayerPosition))
        {
            ReturnToNormalBehaviour();
            return;
        }
        if(!BeginMove(Phase.Returning, _destination,_owner.PeekmoveTimeout))
        {
            ReturnToNormalBehaviour();
        }
    }

    private EnemyCoverSensor.PeekSide SelectPreferredPeekSide()
    {
        if (!_hasPreviousPeekSide)
            return UnityEngine.Random.value < InitialLeftPeekChance ? EnemyCoverSensor.PeekSide.Left : EnemyCoverSensor.PeekSide.Right;
        bool switchSide = UnityEngine.Random.value < _owner.PeekSideSwithChance;
        if(!switchSide)
            return _previousPeekSide;
        return _previousPeekSide == EnemyCoverSensor.PeekSide.Left ? EnemyCoverSensor.PeekSide.Right : EnemyCoverSensor.PeekSide.Left;
    }
    public void Exit()
    {
        if(_entered)
        {
            _owner.PauseAttackCycle();
            _owner.NotifyCoverExited();            
        }

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

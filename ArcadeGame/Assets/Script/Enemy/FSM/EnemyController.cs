using System;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public sealed class EnemyController : MonoBehaviour
{
    private const float MinimumCoverHoldDuration = 0.1f;
    private const float MinimumCoverDecisionInterval = 0.1f;
    private const float MinimumCoverMoveSpeed = 0.1f;
    private const float MinimumFastMoveDistanceGap = 0.5f;

    [Header("Idle")]
    [SerializeField, Min(0f)]
    private float minIdleDuration = 1f;

    [SerializeField, Min(0f)]
    private float maxIdleDuration = 3f;

    [Header("Detection")]
    [SerializeField, Min(0.02f)]
    private float detectionInterval = 0.15f;

    [SerializeField, Min(0.1f)]
    private float detectionRange = 15f;

    [SerializeField]
    private LayerMask sightBlockingMask;

    [SerializeField]
    private Transform eyePoint;

    [SerializeField]
    private Transform target;

    [SerializeField, Min(0f)]
    private float fallbackEyeHeight = 1.5f;

    [SerializeField, Min(0f)]
    private float targetAimHeight = 1f;

    [Header("Field of View")]
    [SerializeField, Range(0f, 360f)]
    private float viewAngle = 120f;

    public float MinIdleDuration => minIdleDuration;
    public float MaxIdleDuration => maxIdleDuration;
    public float DetectionInterval => detectionInterval;
    public float PatrolStoppingDistance => patrolStoppingDistance;

    [Header("Patrol")]
    [SerializeField] private EnemyPatrolRoute patrolRoute;
    [SerializeField, Min(0.05f)] private float patrolStoppingDistance = 0.5f;
    private int _nextPatrolPointIndex;

    [Header("Alert")]
    [SerializeField, Min(0f)] private float alertReactionTime = 0.5f;
    [SerializeField, Min(0.1f)] private float alertMemoryDuration = 3f;
    [SerializeField, Min(0f)] private float alertTrunSpeed = 240f;

    [Header("Chase")]
    [SerializeField, Min(0.02f)] private float chasePathUpdateInterval = 0.2f;
    [SerializeField, Min(0.1f)] private float chaseMemoryDuration = 3f;
    [SerializeField, Min(0f)] private float investigationStoppingDistance = 0.5f;

    [Header("Attack")]
    [SerializeField, Min(0.1f)] private float attackRange = 8f;
    [SerializeField, Min(0f)] private float attackRangeBuffer = 1f;
    [SerializeField, Range(0f, 90f)] private float attackAimTolerance = 10f;

    [Header("Search")]
    [SerializeField, Min(0.1f)] private float searchMoveTimeout = 6f;
    [SerializeField, Min(0.1f)] private float searchLookDuration = 4f;
    [SerializeField, Min(0.1f)] private float searchTurnSpeed = 90f;
    [SerializeField, Min(0.1f)] private float searchStoppingDistance = 0.5f;
    [SerializeField, Min(0.1f)] private float searchChaseRetryDelay = 1f;

    [Header("Reload Decision")]
    [SerializeField] private bool reloadWhenTargetnotVisible = true;
    [SerializeField, Min(0f)] private float reloadAfterLostSightDelay = 2f;

    [Header("Cover")]
    [SerializeField, Min(0.1f)] private float coverMoveSpeed = 6f;
    [SerializeField, Min(0.1f)] private float coverAcceleration = 60f;
    [SerializeField, Min(0.1f)] private float coverMoveTimeOut = 8f;
    [SerializeField, Min(0.1f)] private float coverHoldDuration = 3f;
    [SerializeField, Min(0.1f)] private float coverCheckInterval = 0.25f;

    [Header("Tactical Reload")]
    [SerializeField] private bool seekCoverWhenAmmoLow = true;

    [SerializeField, Range(0f, 1f)] private float lowAmmoRatio = 0.3f;
    [SerializeField, Min(MinimumCoverDecisionInterval)] private float coverRetryInterval = 2f;
    [SerializeField, Min(0.1f)] private float coverReloadTimeoutMargin = 2f;
    [SerializeField, Min(0f)] private float postReloadHoldDuration = 0.35f;

    [Header("Cover Peeking")]
    [SerializeField, Min(1)] private int maxPeekCycles = 2;
    [SerializeField, Min(0.1f)] private float peekMoveTimeout = 2f;
    [SerializeField, Min(0.1f)] private float peekExposureDuration = 1.5f;
    [SerializeField, Min(1)] private int shotsPerPeek = 3;
    [SerializeField, Min(0.1f)] private float peekMemoryDuration = 8f;
    [Tooltip("양쪽 모두 피킹 가능할 때 이전과 반대쪽을 선호할 확률")]
    [SerializeField, Range(0f, 1f)] private float peekSideSwithChance = 0.7f;
    [Tooltip("기존 엄폐 대기 시간에 더하거나 빼는 최대 시간")]
    [SerializeField, Min(0f)] private float coverHoldJitter = 0.8f;
    
    [Header("Close Footstep Reation")]
    [SerializeField, Min(0.1f)] private float closeFootstepDistance = 4f;
    [SerializeField, Min(0.1f)] private float closeFootstepMemoryDuration = 1.5f;
    
    [Header("Cover Fast Movement")]
    [SerializeField, Min(MinimumCoverMoveSpeed)] private float coverFastMoveSpeed = 8f;
    [SerializeField, Min(MinimumFastMoveDistanceGap)] private float coverFastMoveEnterDistance = 5f;
    [SerializeField, Min(MinimumFastMoveDistanceGap)] private float coverFastMoveExitDistance = 2f;
    
    [Header("Hit Reaction")]
    [SerializeField] private HealthSystem healthSystem;
    [SerializeField, Min(0.1f)] private float hitPeekRecoveryDuration = 2f;
    public float HitPeekRecoveryDuration => Mathf.Max(0.1f, hitPeekRecoveryDuration);

    public bool CanReactToHit => isActiveAndEnabled && healthSystem != null && healthSystem.isActiveAndEnabled && healthSystem.IsAlive;

    private float _timeWithoutSight;
    private float _viewDotThreshold;
    private NavMeshAgent _agent;
    private StateMachine _stateMachine;
    private float _attackAimDotThreshold;
    public event Action<Vector3> AttackRequested;
    private const float AttackApproachRatio = 0.8f;
    private const float FacingDirectionSqrEpsilon = 0.0001f;
    private float _remainingPerceptionTime;
    private bool _hasPendingNoise;
    private Vector3 _pendingNoisePosition;
    private EnemyWeapon _weapon;
    private bool _hasStarted;
    private float _nextCoverAttempTime;
    private int _shotsInAttackCycle;
    private float _nextAttackTime;
    private Vector3 _lastheardFootstepPosition;
    private float _lastHeardFootstepTime = float.NegativeInfinity;

    public float SearchMoveTimeout => searchMoveTimeout;
    public float SearchLookDuration => searchLookDuration;
    public float SearchTurnSpeed => searchTurnSpeed;
    public float SearchStoppingDistance => searchStoppingDistance;
    public float SearchChaseRetryDelay => searchChaseRetryDelay;

    public float ChasePathUpdateInterval => chasePathUpdateInterval;
    public float ChaseMemoryDuration => chaseMemoryDuration;

    public float ChaseStoppingDistance => CanSeePlayer ? attackRange * AttackApproachRatio : investigationStoppingDistance;

    public bool HasTarget => target != null && target.gameObject.activeInHierarchy;
    
    public bool CanNaviagate => _agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh;
    public bool IsPathPendinng => CanNaviagate && _agent.pathPending;
    public float CoverMoveSpeed => coverMoveSpeed;
    public float CoverAcceleration => coverAcceleration;
    public float CoverMoveTimeout => coverMoveTimeOut;
    public float CoverHoldDuration => coverHoldDuration;
    public float CoverCheckInterval => coverCheckInterval;
    public bool HasUsableWeapon => _weapon != null && _weapon.IsReadyForAI;
    public bool WeaponNeedsReload => HasUsableWeapon && _weapon.NeedsReload;
    public bool WeaponIsReloading => HasUsableWeapon && _weapon.IsReloading;
    public float CoverReloadWaitLimit => HasUsableWeapon ? _weapon.EstimatedReloadWait + Mathf.Max(0.1f, coverReloadTimeoutMargin) : 0f;
    public float PostReloadHoldDuration => Mathf.Max(0f, postReloadHoldDuration);
    public Vector3 LastKnownAimPosition => LastKnownPlayerPosition + Vector3.up * targetAimHeight;
    public float AttackHoldRange => Mathf.Max(0f, attackRange + attackRangeBuffer);
    public float PeekmoveTimeout => Mathf.Max(0.1f, peekMoveTimeout);
    public float PeekExposureDuration => Mathf.Max(0.1f, peekExposureDuration);
    public int ShotsPerPeek => Mathf.Max(1, shotsPerPeek);
    public int MaxPeekCycles => Mathf.Max(1, maxPeekCycles);
    public float PeekMemoryDuration => Mathf.Max(0.1f, peekMemoryDuration);
    public bool HasLoadedAmmunition => HasUsableWeapon && _weapon.LoadedRounds > 0;
    public float PeekSideSwithChance => Mathf.Clamp01(peekSideSwithChance);
    public float CoverFastMoveSpeed => Mathf.Max(MinimumCoverMoveSpeed, Mathf.Max(coverMoveSpeed, coverFastMoveSpeed));
    public float CoverFastMoveExitDistance => Mathf.Max(MinimumFastMoveDistanceGap, coverFastMoveExitDistance);
    public float CoverFastMoveEnterDistance => Mathf.Max(CoverFastMoveExitDistance + MinimumFastMoveDistanceGap, coverFastMoveEnterDistance);
    
    public EnemyCombatPressure CombatPressure { get; private set; }
    public EnemyStrafeMovement StrafeMovement {get; private set;}
    public IState AttackState { get; private set; }
    public bool CanSeePlayer { get; private set; }
    public Vector3 LastKnownPlayerPosition { get; private set; }
    public float LastStimulusTime { get; private set; }

    public float AlertReactionTime => alertReactionTime;
    public float AlertMemoryDuration => alertMemoryDuration;
    public IState ChaseState { get; private set; }
    public IState SearchState { get; private set; }

    public IState PatrolState { get; private set; }
    public IState AlertState { get; private set; }

    public EnemyIdleState IdleState { get; private set; }

    private WeaponData EquippedWeaponData => _weapon != null ? _weapon.Data : null;

    public bool HasWeaponData => EquippedWeaponData != null;
    public int BurstCount
    {
        get
        {
            WeaponData data = EquippedWeaponData;
            return data != null ? data.ShotsPerCycle : 1;
        }
    }

    public float AttackInterval
    {
        get
        {
            WeaponData data = EquippedWeaponData;
            return data != null ? data.ShotInterval : WeaponData.MinimumShotInterval;
        }
    }

    public float BurstCooldown
    {
        get
        {
            WeaponData data = EquippedWeaponData;
            return data != null ? data.CycleCooldown : WeaponData.MinimumShotInterval;
        }
    }
    public bool AgentUpdatesRotation
    {
        get => _agent != null && _agent.updateRotation;
        set
        {
            if(_agent != null)
            {
                _agent.updateRotation = value;
            }
        }
    }
    public bool ShouldReloadInCover
    {
        get
        {
            if(!HasUsableWeapon)
                return false;
            if(_weapon.IsReloading)
                return true;
            int capacity = _weapon.LoadedCapacity;
            if(capacity <= 0 || !_weapon.NeedsReload)
                return false;
            float ratio = _weapon.LoadedRounds / (float)capacity;
            return ratio <= Mathf.Clamp01(lowAmmoRatio);
        }
    }

    public EnemyCombatDecision CombatDecision {get; private set;}
    public EnemyCoverSensor CoverSensor {get; private set;}
    public EnemyCoverState CoverState {get; private set;}


    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _stateMachine = new StateMachine();

        _viewDotThreshold = Mathf.Cos(viewAngle * 0.5f * Mathf.Deg2Rad);
        _attackAimDotThreshold = Mathf.Cos(attackAimTolerance * Mathf.Deg2Rad);
        IdleState = new EnemyIdleState(this);
        PatrolState = new EnemyPatrolState(this);
        AlertState = new EnemyAlertState(this);
        ChaseState = new EnemyChaseState(this);
        AttackState = new EnemyAttackState(this);
        SearchState = new EnemySearchState(this);
        TryGetComponent(out _weapon);
        TryGetComponent(out EnemyStrafeMovement strafeMovement);
        StrafeMovement = strafeMovement;
        TryGetComponent(out EnemyCombatDecision combatDecision);
        CombatDecision = combatDecision;
        TryGetComponent(out EnemyCoverSensor coverSensor);
        CoverSensor = coverSensor;

        CoverState = new EnemyCoverState(this, _agent, CoverSensor);
        if(healthSystem == null) TryGetComponent(out healthSystem);
        TryGetComponent(out EnemyCombatPressure combatPressure);
        CombatPressure = combatPressure;
    }

    private void Start()
    {
        TryResolvePlayerTarget();
        _hasStarted = true;

        if(_stateMachine.CurrentState == null)
            _stateMachine.Start(IdleState);
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;

        if (CombatPressure != null)
            CombatPressure.TickPressure();

        UpdatePerception(deltaTime);
        UpdateCoverDecision();

        _stateMachine.Tick(deltaTime);
        UpdateReloadDecision(deltaTime);
    }

    private void OnDestroy()
    {
        _stateMachine?.Stop();
    }
    private void OnEnable()
    {
        if(healthSystem != null)
            healthSystem.DamageReceived += HandleDamageReceived;

        PlayerNoise.Emitted += HandlePlayerNoise;
        _remainingPerceptionTime = 0f;
        _timeWithoutSight = 0f;
        //최초 활성화때는 Start가 초기 상태 시작
        //재활성화시 Idle 에서 시작
        if(_hasStarted && _stateMachine != null && _stateMachine.CurrentState == null)
        {
            _stateMachine.Start(IdleState);
        }
    }

    private void OnDisable()
    {
        if(healthSystem != null)
            healthSystem.DamageReceived -= HandleDamageReceived;
            
        PlayerNoise.Emitted -= HandlePlayerNoise;
        PauseAttackCycle();
        _stateMachine?.Stop();
    
        _hasPendingNoise = false;
        CanSeePlayer = false;
        _lastHeardFootstepTime = float.NegativeInfinity;
    }
    
    public void ChangeState(IState nextState)
    {
        if(!ReferenceEquals(nextState, AttackState) && !ReferenceEquals(nextState, CoverState))
        {
            PauseAttackCycle();
        }
        _stateMachine.ChangeState(nextState);
    }

    public void StopMoving()
    {
        if (!CanNaviagate)
        {
            return;
        }

        _agent.isStopped = true;
        _agent.ResetPath();
    }

    public bool CanDetectPlayer()
    {
        if (!TryResolvePlayerTarget())
        {
            return false;
        }

        Vector3 toTarget = target.position - transform.position;

        // 1. 거리 검사
        if (toTarget.sqrMagnitude >
            detectionRange * detectionRange)
        {
            return false;
        }

        // 2. 시야각 검사
        if (!IsWithinViewAngle(toTarget))
        {
            return false;
        }

        // 3. 장애물 검사
        return HasClearLineOfSight();
    }
    public bool TryGetRecentCloseFootstep(out Vector3 position)
    {
        position = default;
        if(!HasTarget)
            return false;
        float age = Time.time - _lastHeardFootstepTime;
        if(age > closeFootstepMemoryDuration)
            return false;
        Vector3 difference = _lastheardFootstepPosition - transform.position;

        float distanceSquared = closeFootstepDistance * closeFootstepDistance;
        if(difference.sqrMagnitude > distanceSquared)
            return false;
        position = _lastheardFootstepPosition;
        return true;
    }

    public bool TryReactToCloseFootstepFromCover()
    {
        if(_stateMachine == null || CoverState == null || !ReferenceEquals(_stateMachine.CurrentState, CoverState))
            return false;
        if(!TryGetRecentCloseFootstep(out Vector3 heardPosition))
            return false;
        IState nextState;
        if(CanSeePlayer)
            nextState = IsTargetWithinAttackHoldRange() ? AttackState : ChaseState;
        else
        {
            LastKnownPlayerPosition = heardPosition;
            LastStimulusTime = _lastHeardFootstepTime;
            nextState = AlertState;
        }
        if(nextState == null)
            return false;
        ChangeState(nextState);
        return true;
    }
    private bool TryResolvePlayerTarget()
    {
        if (target != null)
        {
            return true;
        }

        if (GameManager.Instance == null)
        {
            return false;
        }

        Character player = GameManager.Instance.GetCharacter();

        if (player == null)
        {
            return false;
        }

        target = player.transform;
        return true;
    }

    private bool HasClearLineOfSight()
    {
        Vector3 origin = eyePoint != null
            ? eyePoint.position
            : transform.position + Vector3.up * fallbackEyeHeight;

        Vector3 destination =
            target.position + Vector3.up * targetAimHeight;

        Vector3 direction = destination - origin;
        float distance = direction.magnitude;

        if (distance <= Mathf.Epsilon)
        {
            return true;
        }

        return !Physics.Raycast(
            origin,
            direction / distance,
            distance,
            sightBlockingMask,
            QueryTriggerInteraction.Ignore);
    }

    private bool IsWithinViewAngle(Vector3 toTarget)
    {
        // 높이 차이는 시야각 계산에서 제외
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude <= 0.0001f)
        {
            return true;
        }

        Vector3 forward = transform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 directionToTarget = toTarget.normalized;

        float dot = Vector3.Dot(
            forward,
            directionToTarget);

        return dot >= _viewDotThreshold;
    }
    // Patrol 관련 메서드
    public bool TryGetNextPatrolDestination(out Vector3 destination)
    {
        destination = default;

        if (patrolRoute == null || patrolRoute.Count == 0)
        {
            return false;
        }

        int pointCount = patrolRoute.Count;
        for(int i = 0; i < pointCount; i++)
        {
            int selectedIndex = _nextPatrolPointIndex;
            _nextPatrolPointIndex = (_nextPatrolPointIndex + 1) % pointCount;
            if(patrolRoute.TryGetPosition(selectedIndex, out destination))
            {
                return true;
            }

        }
        return false;
    }
    //지정 목적지 이동 
    public bool TryMoveTo(Vector3 destination, float stoppingDistance)
    {
        if(!CanNaviagate)
        {
            return false;
        }
        _agent.stoppingDistance = Mathf.Max(0.05f, stoppingDistance);
        _agent.isStopped = false;
        return _agent.SetDestination(destination);
    }

    // 도착지 판정
    public bool HasReachedDestination()
    {
        if(!_agent.isOnNavMesh || _agent.pathPending)
        {
            return false;
        }
        if(_agent.remainingDistance > _agent.stoppingDistance)
        {
            return false;
        }
        return !_agent.hasPath || _agent.velocity.sqrMagnitude < 0.01f;
    }

    //경로 실패 
    public bool HasPathFailed()
    {
        if(!_agent.isOnNavMesh || _agent.pathPending)
        {
            return false;
        }
        return _agent.pathStatus != NavMeshPathStatus.PathComplete;
    }

    public void RegisterAlertState(IState alertState)
    {
        AlertState = alertState;
    }

    private bool CanProcessAlertStimuli()
    {
       IState current = _stateMachine.CurrentState;

        return current != null && 
        (ReferenceEquals(current, IdleState) ||
            ReferenceEquals(current, PatrolState) ||
            ReferenceEquals(current, AlertState) ||
            ReferenceEquals(current, ChaseState) ||
            ReferenceEquals(current, SearchState) ||
            ReferenceEquals(current, AttackState) ||
            ReferenceEquals(current, CoverState));
        
    }
    
    private void HandlePlayerNoise(Character source, Vector3 position, float radius,PlayerNoiseType type)
    {
        if(_stateMachine == null || !CanProcessAlertStimuli())
        {
            return;
        }
        if(!TryResolvePlayerTarget() || source == null || source.transform != target)
        {
            return;
        }
        Vector3 difference = position - transform.position;
        if(difference.sqrMagnitude > radius * radius)
        {
            return;
        }
        _pendingNoisePosition = position;
        _hasPendingNoise = true;

        //발소리만 별로도 기억
        //뒤이어 총소리가 발생해도 발소리 기록은 그대로
        if(type == PlayerNoiseType.Footstep)
        {
            _lastheardFootstepPosition = position;
            _lastHeardFootstepTime = Time.time;
        }
    }

    private void UpdatePerception(float deltaTime)
    {
        if(!CanProcessAlertStimuli())
        {
            _hasPendingNoise = false;
            CanSeePlayer = false;
            return;
        }
        _remainingPerceptionTime -= deltaTime;
        if(_remainingPerceptionTime > 0f && !_hasPendingNoise)
        {
            return;
        }
        _remainingPerceptionTime = detectionInterval;
        CanSeePlayer = CanDetectPlayer();

        bool perceivedSomething  = false;
        if (CanSeePlayer)
        {
            LastKnownPlayerPosition = target.position;
            perceivedSomething = true;
        }
        else if(_hasPendingNoise)
        {
            LastKnownPlayerPosition = _pendingNoisePosition;
            perceivedSomething = true;  
        }
        _hasPendingNoise = false;
        if(!perceivedSomething)
        {
            return;
        }
        LastStimulusTime = Time.time;
        if(_stateMachine.IsInState(IdleState) || _stateMachine.IsInState(PatrolState))
        {
            _stateMachine.ChangeState(AlertState);
        }
    }
    private void UpdateReloadDecision(float deltaTime)
    {
        if(_stateMachine != null && ReferenceEquals(_stateMachine.CurrentState, CoverState))
        {
            _timeWithoutSight = 0f;
            return;
        }
        if(!reloadWhenTargetnotVisible || _weapon == null || !_weapon.isActiveAndEnabled)
        {
            _timeWithoutSight = 0f;
            return;
        }
        if (CanSeePlayer)
        {
            _timeWithoutSight = 0f;
            return;
        }
        float delay = Mathf.Max(0f, reloadAfterLostSightDelay);
        _timeWithoutSight = Mathf.Min(_timeWithoutSight + Mathf.Max(0f, deltaTime),delay);
        if(_timeWithoutSight < delay) return;
        if(_weapon.CanStartReload) _weapon.TryStartReload();
    }

    private void UpdateCoverDecision()
    {
        if(!seekCoverWhenAmmoLow || !HasUsableWeapon || !WeaponNeedsReload || !HasTarget || !CanSeePlayer || CoverState == null || _stateMachine == null)
            return;
        IState current = _stateMachine.CurrentState;

        if(!ReferenceEquals(current, AttackState) && !ReferenceEquals(current, ChaseState))
            return;
        if(Time.time < _nextCoverAttempTime)
            return;
        int capacity = _weapon.LoadedCapacity;

        if(capacity <= 0)
            return;
        float ammoRatio = _weapon.LoadedRounds / (float)capacity;

        if(ammoRatio > Mathf.Clamp01(lowAmmoRatio))
            return;
        _nextCoverAttempTime = Time.time + Mathf.Max(MinimumCoverDecisionInterval , coverRetryInterval);
        TryEnterCover(reloadOnArrival: true);

    }
    public void FaceLastKnownPosition(float deletaTime)
    {
        Vector3 direction = LastKnownPlayerPosition - transform.position;
        direction.y = 0f;
        if(direction.sqrMagnitude <= FacingDirectionSqrEpsilon)
        {
            return;
        }
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            alertTrunSpeed * deletaTime);
    }
    public bool IsTargetInAttackRange()
    {
        if(!HasTarget || !CanSeePlayer)
        {
            return false;
        }
        Vector3 difference = target.position - transform.position;

        return difference.sqrMagnitude <= attackRange * attackRange;
    }

    public bool IsTargetWithinAttackHoldRange()
    {
        if(!HasTarget)
        {
            return false;
        }
        float holdRange = attackRange + attackRangeBuffer;
        Vector3 difference = target.position - transform.position;
        return difference.sqrMagnitude <= holdRange * holdRange;
    }
    public bool TryRequestAttack()
    {
        if(Time.time < _nextAttackTime)
            return false;

        if(!HasWeaponData || _weapon == null || !_weapon.CanRequestFire  || !HasTarget || !CanSeePlayer || !IsTargetWithinAttackHoldRange())
        {
            return false;
        }
        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;
        if(toTarget.sqrMagnitude > FacingDirectionSqrEpsilon)
        {
            Vector3 forward = transform.forward;
            forward.y = 0f;
            if(forward.sqrMagnitude <= FacingDirectionSqrEpsilon)
            {
                return false;
            }
            float facingDot = Vector3.Dot(forward.normalized, toTarget.normalized);
            if(facingDot < _attackAimDotThreshold)
            {
                return false;
            }
        }
        if (!CanDetectPlayer())
        {
            return false;
        }
        Vector3 aimPosition = target.position + Vector3.up * targetAimHeight;
        if(!_weapon.HasClearShot(aimPosition, sightBlockingMask))
            return false;
        if(!_weapon.TryFire(aimPosition))
            return false;
        RecordSuccessfulAttack();
        return true;
    }

    public bool TryEnterCover(bool reloadOnArrival = false)
    {
        if(!isActiveAndEnabled || _stateMachine == null || _stateMachine.CurrentState == null || CoverState == null || ReferenceEquals(_stateMachine.CurrentState, CoverState) || !HasTarget || !CanSeePlayer)
            return false;
        if(TryGetRecentCloseFootstep(out _))
            return false;
        IState current = _stateMachine.CurrentState;
        if(!ReferenceEquals(current, AttackState) && !ReferenceEquals(current, ChaseState))
            return false;
        if(!CoverState.TryPrepare(reloadOnArrival))
            return false;
        ChangeState(CoverState);
        return true;
    }

    public bool TryStartCoverReload()
    {
        return HasUsableWeapon && _weapon.CanStartReload && _weapon.TryStartReload();
    }

    public void NotifyCoverExited()
    {
        _nextCoverAttempTime = Time.time + Mathf.Max(MinimumCoverDecisionInterval, coverRetryInterval);
    }

    public void PauseAttackCycle()
    {
        if(_shotsInAttackCycle > 0)
        {
            _nextAttackTime = Mathf.Max(_nextAttackTime, Time.time + Mathf.Max(WeaponData.MinimumShotInterval, BurstCooldown));
        }
        _shotsInAttackCycle = 0;
    }
    private void RecordSuccessfulAttack()
    {
        _shotsInAttackCycle++;
        if(_shotsInAttackCycle >= Mathf.Max(1, BurstCount))
        {
            _shotsInAttackCycle = 0;
            _nextAttackTime = Time.time + Mathf.Max(WeaponData.MinimumShotInterval, BurstCooldown);
            return;
        }
        _nextAttackTime = Time.time + Mathf.Max(WeaponData.MinimumShotInterval, BurstCooldown);
    }

    public float SampleCoverHoldDuration()
    {
        float baseDuration = Mathf.Max(MinimumCoverHoldDuration, coverHoldDuration);
        float jitter = Mathf.Max(0f, coverHoldJitter);
        float minimum = Mathf.Max(MinimumCoverHoldDuration,baseDuration - jitter);
        float maximum = baseDuration + jitter;
        return UnityEngine.Random.Range(minimum, maximum);
    }

    private void HandleDamageReceived(DamageInfo damageInfo)
    {
        if(!CanReactToHit)
            return;
        if(CombatPressure != null)
            CombatPressure.RegisterHit();
        
        if( _stateMachine == null || CoverState == null )
            return;

        if(!ReferenceEquals(_stateMachine.CurrentState, CoverState))
            return;

        CoverState.QueueHitReaction();
    }
#if UNITY_EDITOR
    private void OnValidate()
    {
        maxIdleDuration = Mathf.Max(minIdleDuration, maxIdleDuration);
        _viewDotThreshold = Mathf.Cos(viewAngle * 0.5f * Mathf.Deg2Rad);
        _attackAimDotThreshold = Mathf.Cos(attackAimTolerance * Mathf.Deg2Rad);
    }

    private const string EnterCoverTestMenu = "Test/Enter Cover";
    [ContextMenu(EnterCoverTestMenu)]
    private void TestEnterCover()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Play 모드에서 실행하세요.", this);
            return;
        }

        bool entered = TryEnterCover();

        Debug.Log(
            entered
                ? "엄폐 상태 진입: 후보 예약 완료"
                : "엄폐 진입 실패: 전투 상태, 시야, 후보 조건을 확인하세요.",
            this);
    }

    private const string SimulatePeekHitMenu = "Test/Simulate Peek Hit";

    [ContextMenu(SimulatePeekHitMenu)]
    private void TestSimulatePeekHit()
    {
        if(!Application.isPlaying || !CanReactToHit || _stateMachine == null || CoverState == null || !ReferenceEquals(_stateMachine.CurrentState, CoverState))
        {
            Debug.LogWarning("Play 모드에서 살아 있는 적이 엄폐 상태일 때 실행하세요.", this);
            return;
        }

        bool accepted = CoverState.QueueHitReaction();
        Debug.Log(accepted ? "피격 반응 예약 완료. 다음 Tick에서 엄폐 복귀를 시도합니다." : "현재는 피킹 이동 또는 피킹 사격 단계가 아닙니다.", this);
    }

    private const string SimulateCombatHitMenu =
    "Test/Simulate Combat Hit";

    [ContextMenu(SimulateCombatHitMenu)]
    private void TestSimulateCombatHit()
    {
        if (!Application.isPlaying ||
            !CanReactToHit ||
            CombatPressure == null ||
            !CombatPressure.isActiveAndEnabled)
        {
            Debug.LogWarning(
                "Play 모드에서 HealthSystem과 EnemyCombatPressure 연결을 확인하세요.",
                this);
            return;
        }

        // 실제 체력을 감소시키지 않고 AI의 피격 수신 경로만 테스트.
        HandleDamageReceived(default);

        Debug.Log(
            $"Pressure: {CombatPressure.PressureNormalized:F2}, " +
            $"High Pressure: {CombatPressure.IsHighPressure}",
            this);
    }
#endif

}

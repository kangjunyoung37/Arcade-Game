using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public sealed class EnemyController : MonoBehaviour
{
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

    private float _viewDotThreshold;

    private NavMeshAgent _agent;
    private StateMachine _stateMachine;

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
    [SerializeField] float attackInterval = 0.5f;
    [SerializeField, Min(0f)] private float attackRangeBuffer = 1f;
    [SerializeField, Range(0f, 90f)] private float attackAimTolerance = 10f;

    private readonly Dictionary<GameObject, Bullet> _bulletCache = new Dictionary<GameObject, Bullet>();
    private float _attackAimDotThreshold;
    public float AttackInterval =>attackInterval;
    public event Action<Vector3> AttackRequested;

    private const float AttackApproachRatio = 0.8f;

    public float ChasePathUpdateInterval => chasePathUpdateInterval;
    public float ChaseMemoryDuration => chaseMemoryDuration;

    public float ChaseStoppingDistance => CanSeePlayer ? attackRange * AttackApproachRatio : investigationStoppingDistance;

    public bool HasTarget => target != null && target.gameObject.activeInHierarchy;
    
    public bool CanNaviagate => _agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh;
    public bool IsPathPendinng => CanNaviagate && _agent.pathPending;

    public IState AttackState { get; private set; }

    private const float FacingDirectionSqrEpsilon = 0.0001f;

    private float _remainingPerceptionTime;
    private bool _hasPendingNoise;
    private Vector3 _pendingNoisePosition;

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

    }

    private void Start()
    {
        TryResolvePlayerTarget();
        _stateMachine.Start(IdleState);
    }

    private void Update()
    {
        UpdatePerception(Time.deltaTime);
        _stateMachine.Tick(Time.deltaTime);
    }

    private void OnDestroy()
    {
        _stateMachine.Stop();
    }
    private void OnEnable()
    {
        PlayerNoise.Emitted += HandlePlayerNoise;
        _remainingPerceptionTime = 0f;
    }

    private void OnDisable()
    {
        PlayerNoise.Emitted -= HandlePlayerNoise;
        _hasPendingNoise = false;
        CanSeePlayer = false;
    }
    
    public void ChangeState(IState nextState)
    {
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
        return current != null && (ReferenceEquals(current, IdleState) || ReferenceEquals(current, PatrolState) || ReferenceEquals(current, AlertState) || ReferenceEquals(current, ChaseState)||ReferenceEquals(current, SearchState) || ReferenceEquals(current, AttackState));
    }
    
    private void HandlePlayerNoise(Character source, Vector3 position, float radius)
    {
        if(!CanProcessAlertStimuli())
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
        Action<Vector3> handler = Attackrequested;
        if(handler == null || !HasTarget || !CanSeePlayer || !IsTargetWithinAttackHoldRange())
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
        handler.Invoke(aimPosition);
        return true;
    }
#if UNITY_EDITOR
    private void OnValidate()
    {
        maxIdleDuration = Mathf.Max(minIdleDuration, maxIdleDuration);
        _viewDotThreshold = Mathf.Cos(viewAngle * 0.5f * Mathf.Deg2Rad);
        _attackAimDotThreshold = Mathf.Cos(attackAimTolerance * Mathf.Deg2Rad);
    }
#endif

}

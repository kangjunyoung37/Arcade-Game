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

    public IState PatrolState { get; private set; }
    public IState AlertState { get; private set; }

    public EnemyIdleState IdleState { get; private set; }

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _stateMachine = new StateMachine();

        _viewDotThreshold = Mathf.Cos(viewAngle * 0.5f * Mathf.Deg2Rad);
        IdleState = new EnemyIdleState(this);
    }

    private void Start()
    {
        TryResolvePlayerTarget();
        _stateMachine.Start(IdleState);
    }

    private void Update()
    {
        _stateMachine.Tick(Time.deltaTime);
    }

    private void OnDestroy()
    {
        _stateMachine.Stop();
    }

    public void ChangeState(IState nextState)
    {
        _stateMachine.ChangeState(nextState);
    }

    public void StopMoving()
    {
        if (!_agent.isOnNavMesh)
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
    public void RegisterPatrolState(IState patrolState)
    {
        PatrolState = patrolState;
    }

    public void RegisterAlertState(IState alertState)
    {
        AlertState = alertState;
    }
}

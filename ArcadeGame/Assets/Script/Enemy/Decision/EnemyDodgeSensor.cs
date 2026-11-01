using UnityEngine.AI;
using UnityEngine;
using Unity.VisualScripting;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyController))]
[RequireComponent(typeof(NavMeshAgent))]
public sealed class EnemyDodgeSensor : MonoBehaviour
{
    private const float MinimumDistance = 0.01f;
    private const float DirectionSqrEpsilon = 0.0001f;
    private const float CapsuleDiameterMultiplier = 2f;
    private const float FirstSideProbability = 0.5f;

    [Header("Doge Distance")]
    [SerializeField, Min(MinimumDistance)] private float dodgeDistance = 3f;
    [SerializeField, Min(MinimumDistance)] private float minimumDodgeDistance = 1.5f;

    [Header("NavMesh")]
    [SerializeField,Min(MinimumDistance)] private float sampleRadius = 0.25f;
    [SerializeField, Min(0f)] private float maxHeightDifference = 0.2f;

    [Header("Collision")]
    [Tooltip("벽, 상자 등 이동을 막는 환경 레이어, 바닥과 자기 자신은 제외")]
    [SerializeField] private LayerMask blockingMask;

    [SerializeField, Min(0f)] private float clearance = 0.03f;
    
    [Header("Threat")]
    [SerializeField, Min(0f)] private float minimumThreatDistance = 2f;

    private EnemyController _owner;
    private NavMeshAgent _agent;

    private bool CanNavigate => _agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh;
    private Vector3 GroundPosition => _agent.nextPosition - Vector3.up * _agent.baseOffset;

    private void Awake()
    {
        _owner = GetComponent<EnemyController>();
        _agent = GetComponent<NavMeshAgent>();
    }

    public bool TryFindDodgePosition(out Vector3 destination)
    {
        destination = default;

        if(!isActiveAndEnabled || _owner == null || !CanNavigate || blockingMask.value == 0 || !_owner.HasTarget || !_owner.CanSeePlayer)
            return false;
        
        Vector3 origin = GroundPosition;

        if(!HasStandingSpace(origin))
            return false;
        
        Vector3 forward = _owner.LastKnownPlayerPosition - origin;
        forward.y = 0;
        if(forward.sqrMagnitude <= DirectionSqrEpsilon)
            return false;
        
        forward.Normalize();
        Vector3 side = Vector3.Cross(Vector3.up, forward);

        if(Random.value < FirstSideProbability)
            side = -side;
        
        NavMeshQueryFilter filter = new NavMeshQueryFilter
        {
            agentTypeID = _agent.agentTypeID,
            areaMask = _agent.areaMask
        };

        if(TryCandidate(origin, side, filter, out destination))
            return true;
        
        if(TryCandidate(origin, -side, filter, out destination))
            return true;

        //좌우가 막혔으면 뒤쪽 대각선
        Vector3 backwardSide = (side - forward).normalized;

        if(TryCandidate(origin, backwardSide, filter, out destination))
            return true;
        
        Vector3 oppositeBackwardSide = (-side - forward).normalized;

        return TryCandidate(origin, oppositeBackwardSide, filter, out destination);
    }

    private bool TryCandidate(Vector3 origin, Vector3 direction, NavMeshQueryFilter filter, out Vector3 destination)
    {
        destination = default;
        float distance = Mathf.Max(MinimumDistance, dodgeDistance);
        float sampling = Mathf.Max(MinimumDistance, sampleRadius);

        Vector3 candidate = origin + direction * distance;

        if(!NavMesh.SamplePosition(candidate, out NavMeshHit hit, sampling, filter))
            return false;
        
        Vector3 sampled = hit.position;

        Vector3 movement = sampled - origin;
        float distanceSquared = movement.sqrMagnitude;

        float minimum = Mathf.Clamp(minimumDodgeDistance, MinimumDistance, distance);

        float maximum = distance + sampling;
        
        if(distanceSquared < minimum * minimum || distanceSquared > maximum * maximum)
            return false;
        
        Vector3 threatOffset = sampled - _owner.LastKnownPlayerPosition;
        threatOffset.y = 0f;

        float threatDistance = Mathf.Max(0f, minimumThreatDistance);

        if(threatOffset.sqrMagnitude < threatDistance * threatDistance)
            return false;
        
        //장애물을 우회하는 경로가 아닌 직접 이동만 허용
        if(NavMesh.Raycast(origin, sampled, out _, filter))
            return false;
        
        if(!HasStandingSpace(sampled))
            return false;
        
        GetCapsule(origin, out Vector3 bottom, out Vector3 top, out float radius);

        float travelDistance = Mathf.Sqrt(distanceSquared);

        if(Physics.CapsuleCast(bottom, top, radius, movement / travelDistance, travelDistance, blockingMask, QueryTriggerInteraction.Ignore))
            return false;
        destination = sampled;
        return true;
    }

    private bool HasStandingSpace(Vector3 groundPosition)
    {
        GetCapsule(groundPosition, out Vector3 bottom, out Vector3 top, out float radius);

        return !Physics.CheckCapsule(bottom, top, radius, blockingMask, QueryTriggerInteraction.Ignore);
    }

    private void GetCapsule(Vector3 groundPosition, out Vector3 bottom, out Vector3 top, out float radius)
    {
        radius = Mathf.Max(MinimumDistance, _agent.radius + Mathf.Max(0f, clearance));

        float height = Mathf.Max(_agent.height, radius * CapsuleDiameterMultiplier);

        bottom = groundPosition + Vector3.up * radius;
        top = groundPosition + Vector3.up * (height - radius);

    }

    public bool CanTraverseDodgeSegment( Vector3 origin, Vector3 destination)
    {
        if (!isActiveAndEnabled || !CanNavigate || blockingMask.value == 0)
            return false;

        // 시작 위치에 이미 장애물이 겹쳤는지도 확인.
        if (!HasStandingSpace(origin) || !HasStandingSpace(destination))
            return false;

        NavMeshQueryFilter filter = new NavMeshQueryFilter
        {
            agentTypeID = _agent.agentTypeID,
            areaMask = _agent.areaMask
        };

        if (NavMesh.Raycast(origin, destination, out _, filter))
            return false;

        Vector3 movement = destination - origin;
        float distanceSquared = movement.sqrMagnitude;

        if (distanceSquared <= DirectionSqrEpsilon)
            return true;

        GetCapsule( origin, out Vector3 bottom, out Vector3 top, out float radius);

        float distance = Mathf.Sqrt(distanceSquared);

        return !Physics.CapsuleCast( bottom, top, radius, movement / distance, distance, blockingMask, QueryTriggerInteraction.Ignore);
    }    
       
#if UNITY_EDITOR
    private const string TestMenu = "Test/Find Dodge Position";
    private const float TestLineDuration = 5f;
    private const float TestMarkerRadius = 0.15f;

    [Header("Editor Test Result")]
    [SerializeField] private bool testFound;
    [SerializeField] private Vector3 testDestination;

    [ContextMenu(TestMenu)]
    private void TestFindDodgePosition()
    {
        testFound = false;
        testDestination = default;

        if (!Application.isPlaying)
        {
            Debug.LogWarning("Play 모드에서 실행하세요.", this);
            return;
        }

        testFound = TryFindDodgePosition(out testDestination);

        if (!testFound)
        {
            Debug.LogWarning(
                "회피 후보 없음. 시야, NavMesh, Blocking Mask, " +
                "출발점과 이동 구간의 공간을 확인하세요.",
                this);
            return;
        }

        Debug.DrawLine(
            GroundPosition,
            testDestination,
            Color.cyan,
            TestLineDuration);

        Debug.Log(
            $"회피 목적지 선택 성공: {testDestination}",
            this);
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || !testFound)
            return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(testDestination, TestMarkerRadius);
    }
#endif
}


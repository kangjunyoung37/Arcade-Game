using UnityEngine.AI;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyController))]
[RequireComponent(typeof(NavMeshAgent))]
public sealed class EnemyCoverSensor : MonoBehaviour
{
    private enum CoverEvaluation
    {
        Valid,
        Unavailable,
        OutsideSearchRadius,
        NavMeshNotFound,
        HeightMismatch,
        NoStandingSpace,
        TooCloseToThreat,
        Exposed,
        NoCompletePath,
        PathCornersUnavailable,
        PathTooLong
    }

    private const float MinimumDistance = 0.01f;
    private const int PathCornerCapacity = 64;
    private const float CapsuleDiameterMultiplier = 2f;
    private const int CandidiateCapacity = 32;

    private int _candidatedCount;

  

    [Header("Candidates")]
    [SerializeField, Min(MinimumDistance)] private float searchRadius = 12f;

    [Header("Occlusion")]
    [Tooltip("벽, 상자 등 시야를 차단하는 환경 레이어만 포함")]
    [SerializeField] private LayerMask coverBlockingMask;

    [SerializeField, Min(MinimumDistance)] private float bodyHeight = 0.9f;
    [SerializeField, Min(MinimumDistance)] private float headHeight = 1.6f;

    [Tooltip("플레이어의 마지막 위치에 더하는 관측 높이")]
    [SerializeField, Min(MinimumDistance)] private float threatEyeHeight = 1.5f;
    
    [Header("NavMesh")]
    [SerializeField, Min(MinimumDistance)] private float sampleRadius = 0.3f;
    [SerializeField, Min(0f)] private float maxHeightDifference = 0.3f;
   
    [Header("Cover Scoring")]
    [SerializeField, Min(MinimumDistance)] private float maxPathLength = 24f;
    [SerializeField, Min(MinimumDistance)] private float preferredThreatDistance = 6f;
    [SerializeField, Min(0f)] private float minimumThreatDistance = 4f;
    [SerializeField, Min(0f)] private float pathLengthWeight = 1f;
    [SerializeField, Min(0f)] private float preferredDistanceWeight = 2f;


    private readonly Vector3[] _pathCorners = new Vector3[PathCornerCapacity];
    private readonly CoverPoint[] _nearbyCandidates = new CoverPoint[CandidiateCapacity];
    private readonly float[] _candidateDistancesSquared = new float[CandidiateCapacity];
    private EnemyController _owner;
    private NavMeshAgent _agent;
    private NavMeshPath _queryPath;
    public int LastCandidateCount => _candidatedCount;
    public bool LastQueryTruncated{get; private set;}

    private void Awake()
    {
        _owner = GetComponent<EnemyController>();
        _agent = GetComponent<NavMeshAgent>();

        //탐색할 때마다 생성하지 않고 재사용
        _queryPath = new NavMeshPath();
    }

    public bool TryFindCover(Vector3 lastKnownThreatPosition, out CoverPoint selectedPoint, out Vector3 destination)
    {
        selectedPoint = null;
        destination = default;
        
        _candidatedCount = 0;
        LastQueryTruncated = false;

        if(!isActiveAndEnabled || _agent == null || !_agent.isActiveAndEnabled || !_agent.isOnNavMesh || _queryPath == null || coverBlockingMask.value == 0) return false;

        Vector3 origin = _agent.nextPosition - Vector3.up * _agent.baseOffset;
        _candidatedCount = CoverRegistry.QueryNearbyNonAlloc(origin,Mathf.Max(MinimumDistance, searchRadius), _owner,_nearbyCandidates,_candidateDistancesSquared, out bool truncated);

        LastQueryTruncated = truncated;

        float bestCost = float.PositiveInfinity;

        for(int i = 0; i < _candidatedCount; i++)
        {
            CoverPoint point = _nearbyCandidates[i];

            CoverEvaluation result = EvaluateCandidate(point, lastKnownThreatPosition, out Vector3 candidatePosition, out float cost);
            if(result != CoverEvaluation.Valid || cost >= bestCost)
                continue;
            bestCost = cost;
            selectedPoint = point;
            destination = candidatePosition;
        }
        return selectedPoint != null;
    }

    private CoverEvaluation EvaluateCandidate(CoverPoint point, Vector3 threatPosition, out Vector3 destination, out float cost)
    {
        destination = default;
        cost = float.PositiveInfinity;

        if(point == null || !point.IsAvailableFor(_owner)) return CoverEvaluation.Unavailable;
        Vector3 origin = _agent.nextPosition - Vector3.up * _agent.baseOffset;
        
        float radius = Mathf.Max(MinimumDistance, searchRadius);
        float radiuSquared = radius * radius;

        if((point.Position - origin).sqrMagnitude > radiuSquared)
            return CoverEvaluation.OutsideSearchRadius;
        NavMeshQueryFilter filter = new NavMeshQueryFilter
        {
            agentTypeID = _agent.agentTypeID,
            areaMask = _agent.areaMask
        };
        if(!NavMesh.SamplePosition(point.Position, out NavMeshHit hit,sampleRadius, filter))
            return CoverEvaluation.NavMeshNotFound;
        Vector3 candidate = hit.position;

        if(Mathf.Abs(candidate.y - point.Position.y) > maxHeightDifference)
            return CoverEvaluation.HeightMismatch;
        
        if((candidate - origin).sqrMagnitude > radiuSquared)
            return CoverEvaluation.OutsideSearchRadius;
        
        if(!HasStandingSpace(candidate))
            return CoverEvaluation.NoStandingSpace;
        
        Vector3 threatOffset = candidate - threatPosition;
        threatOffset.y = 0f;

        float minimumDistance = Mathf.Max(0f, minimumThreatDistance);

        if(threatOffset.sqrMagnitude < minimumDistance * minimumDistance)
            return CoverEvaluation.TooCloseToThreat;

        Vector3 threatEye = threatPosition + Vector3.up * threatEyeHeight;

        if(!IsProtected(candidate, threatEye))
            return CoverEvaluation.Exposed;
        
        if(!_agent.CalculatePath(candidate, _queryPath) || _queryPath.status != NavMeshPathStatus.PathComplete)
            return CoverEvaluation.NoCompletePath;

        if(!TryGetPathLength(out float pathLength))
            return CoverEvaluation.PathCornersUnavailable;
        
        if(pathLength > Mathf.Max(MinimumDistance, maxPathLength))
            return CoverEvaluation.PathTooLong;

        float  preferredDistance = Mathf.Max(minimumDistance, preferredThreatDistance);

        float distanceError = Mathf.Abs(threatOffset.magnitude - preferredDistance);
        
        cost = pathLength * Mathf.Max(0f, pathLengthWeight) + distanceError * Mathf.Max(0f, preferredDistanceWeight);

        destination = candidate;
        return CoverEvaluation.Valid;
    }

    private bool HasStandingSpace(Vector3 groundPosition)
    {
        float radius = Mathf.Max(MinimumDistance, _agent.radius);
        float height = Mathf.Max(_agent.height, radius * CapsuleDiameterMultiplier);

        Vector3 bottom = groundPosition + Vector3.up * radius;
        Vector3 top = groundPosition + Vector3.up * (height - radius);

        return !Physics.CheckCapsule(bottom, top, radius, coverBlockingMask, QueryTriggerInteraction.Ignore);
    }
    private bool IsProtected(Vector3 groundPosition, Vector3 threatEye)
    {
        Vector3 bodyPosition = groundPosition + Vector3.up * bodyHeight;
        Vector3 headPosition = groundPosition + Vector3.up * headHeight;

        return IsBlocked(bodyPosition, threatEye) && IsBlocked(headPosition, threatEye);
    }

    private bool TryGetPathLength(out float length)
    {
        length = 0f;
        int count = _queryPath.GetCornersNonAlloc(_pathCorners);

        // 짧은 경로로 잘못 평가하지 않고 보수적으로 제외한다.
        if(count < 2 || count >= _pathCorners.Length) 
            return false;
        for(int i = 1; i < count; i++)
        {
            length += Vector3.Distance(_pathCorners[i - 1], _pathCorners[i]);
        }
        return true;
    }
    private bool IsBlocked(Vector3 origin, Vector3 destination)
    {
        Vector3 diretion = destination - origin;
        float distance = diretion.magnitude;
        if(distance <= MinimumDistance) return false;

        return Physics.Raycast(origin, diretion / distance, distance, coverBlockingMask, QueryTriggerInteraction.Ignore);
    }

    public bool IsCoverPositionSafe(Vector3 groundPosition, Vector3 lastKnownThreatPosition)
    {
        if(!isActiveAndEnabled || _agent == null || !_agent.isActiveAndEnabled || !_agent.isOnNavMesh || coverBlockingMask.value == 0)
            return false;
        Vector3 offset = groundPosition - lastKnownThreatPosition;
        offset.y = 0f;
        float minimumDistance = Mathf.Max(0f, minimumThreatDistance);

        if(offset.sqrMagnitude < minimumDistance * minimumDistance)
            return false;
        if(!HasStandingSpace(groundPosition))
            return false;
        Vector3 threatEye = lastKnownThreatPosition + Vector3.up * threatEyeHeight;

        return IsProtected(groundPosition, threatEye);

    }

#if UNITY_EDITOR
    private const string TestMenuName = "Test/Find Cover";
    private const float MarkerRadius = 0.2f;
    [Header("Editor Test Result")]
    [SerializeField] private bool testFoundCover;
    [SerializeField] private CoverPoint testSelectedPoint;
    [SerializeField] private Vector3 testDestination;


    [ContextMenu(TestMenuName)]
    private void TestFindCover()
    {
        testFoundCover = false;
        testSelectedPoint = null;
        testDestination = default;

        if (!Application.isPlaying)
        {
            Debug.LogWarning("엄폐 검사 중단: Play 모드가 아닙니다.", this);
            return;
        }

        if (_owner == null || !_owner.HasTarget)
        {
            Debug.LogWarning("엄폐 검사 중단: 플레이어 타깃이 없습니다.", this);
            return;
        }

        if (!_owner.CanSeePlayer)
        {
            Debug.LogWarning(
                "엄폐 검사 중단: 현재 적이 플레이어를 보고 있지 않습니다.",
                this);
            return;
        }

        if (!isActiveAndEnabled ||
            _agent == null ||
            !_agent.isActiveAndEnabled ||
            !_agent.isOnNavMesh ||
            _queryPath == null)
        {
            Debug.LogWarning(
                "엄폐 검사 중단: 센서/Agent가 비활성 상태이거나 " +
                "NavMesh 위에 없거나 초기화되지 않았습니다.",
                this);
            return;
        }

        if (coverBlockingMask.value == 0)
        {
            Debug.LogWarning(
                "엄폐 검사 중단: Cover Blocking Mask를 설정하세요.",
                this);
            return;
        }

        Vector3 threatPosition = _owner.LastKnownPlayerPosition;

        testFoundCover = TryFindCover(
            threatPosition,
            out testSelectedPoint,
            out testDestination);

         Debug.Log(
            $"등록된 전체 후보: {CoverRegistry.Count}, " +
            $"이번 검사 후보: {_candidatedCount}",
            this);

        if (LastQueryTruncated)
        {
            Debug.LogWarning(
                "주변 후보가 버퍼 용량을 초과했습니다. " +
                "가까운 후보 일부만 평가한 결과입니다.",
                this);
        }

        for (int i = 0; i < _candidatedCount; i++)
        {
            CoverPoint point = _nearbyCandidates[i];
            string reason = DiagnoseCoverPoint(point, threatPosition);

            Debug.Log(
                $"엄폐 후보 [{i}] {point?.name}: {reason}",
                point != null ? (UnityEngine.Object)point : this);
        }
    }

    private const float DiagnosticLineDuration = 10f;
   private string DiagnoseCoverPoint( CoverPoint point, Vector3 threatPosition)
    {
        CoverEvaluation result = EvaluateCandidate(
            point,
            threatPosition,
            out _,
            out float cost);

        return result == CoverEvaluation.Valid
            ? $"선택 가능 / 비용: {cost:F2} (낮을수록 유리)"
            : $"제외 이유: {result}";
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        for (int i = 0; i < _candidatedCount; i++)
        {
            CoverPoint point = _nearbyCandidates[i];

            if (point != null)
                Gizmos.DrawWireSphere(point.Position, MarkerRadius);
        }

        if (!Application.isPlaying || !testFoundCover)
            return;

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(testDestination, MarkerRadius);
        Gizmos.DrawLine(transform.position, testDestination);
    }
#endif
}

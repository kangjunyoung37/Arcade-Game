using UnityEngine;
using UnityEngine.AI;
[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyController))]
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyStrafeMovement : MonoBehaviour
{
    private const float DirectionSqrEpsion = 0.0001f;
    private const float MinimumInterval = 0.1f;
    private const float MaximumSweepAngle = 30f;
    private const float MinimumTravelMultiplier = 2f;
    [Header("Movement")]
    [SerializeField, Min(0.1f)] private float moveSpeed = 2f;
    [SerializeField, Min(0.1f)] private float strafeDistance = 2f;
    [SerializeField, Min(0.1f)] private float stoppingDistance = 0.15f;
    [SerializeField, Min(0.1f)] private float moveTimeout = 2.5f;
    [Header("Pause Between Moves")]
    [SerializeField, Min(MinimumInterval)] private float minPauseDuration = 0.25f;
    [SerializeField, Min(MinimumInterval)] private float maxPauseDuration = 0.6f;
    [Header("NavMesh Validation")]
    [SerializeField, Min(0.05f)] private float sampleRadius = 0.4f;
    [SerializeField, Min(0f)] private float maxHeightDifference = 0.5f;
    private EnemyController _owner;
    private NavMeshAgent _agent;
    private bool _running;
    private bool _moving;
    private int _side;
    
    private float _remainingPause;
    private float _remainingMoveTime;

    private float _previousSpeed;
    private float _previousStoppingDistance;
    private bool _previousUpdateRotation;

    private bool CanNavigate => _agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh;
    private void Awake()
    {
        _owner = GetComponent<EnemyController>();
        _agent = GetComponent<NavMeshAgent>();
    }
    public void Begin()
    {
        if(_running || !isActiveAndEnabled || _owner == null || !CanNavigate) return;
        _previousSpeed = _agent.speed;
        _previousStoppingDistance = _agent.stoppingDistance;
        _previousUpdateRotation = _agent.updateRotation;
        _owner.StopMoving();
        _agent.speed = Mathf.Max(MinimumInterval, moveSpeed);
        _agent.stoppingDistance = Mathf.Max(0.05f, stoppingDistance);
        _agent.updateRotation = false;

        _side = Random.value < 0.5f ? -1 : 1;
        _remainingPause = GetPauseDuration();
        _moving = false;
        _running = true;
    }
    
    public void Tick(float deltaTime)
    {
        if(!_running || _owner == null || !CanNavigate || deltaTime <= 0f) return;
        if(_moving)
        {
            _remainingMoveTime -= deltaTime;
            bool finished = !_agent.pathPending && (!_agent.hasPath || _agent.pathStatus != NavMeshPathStatus.PathComplete || _agent.remainingDistance <= _agent.stoppingDistance);
            if(finished || _remainingMoveTime <= 0f)
            {
                _owner.StopMoving();
                _moving = false;
                _side = -_side;
                _remainingPause = GetPauseDuration();
            }
            return;
        }
        _remainingPause -= deltaTime;
        if(_remainingPause > 0f) return;
        if(TryStartMove(_side)) return;
        if (TryStartMove(-_side))
        {
            _side = -_side;
            return;
        }
        _remainingPause = GetPauseDuration();
    }
    private bool TryStartMove(int side)
    {
        if(!_owner.HasTarget || !_owner.CanSeePlayer) return false;

        Vector3 origin = _agent.nextPosition - Vector3.up * _agent.baseOffset;
        Vector3 center = _owner.LastKnownPlayerPosition;
        
        Vector3 offset = origin - center;
        offset.y = 0f;
        if(offset.sqrMagnitude <= DirectionSqrEpsion) return false;
        float radius = offset.magnitude;
        // 같은 반지름 위에 좌위 지점 선택
        float angle = Mathf.Min(Mathf.Max(0f,strafeDistance) / radius * Mathf.Rad2Deg, MaximumSweepAngle);
        Vector3 candidate = center + Quaternion.AngleAxis(angle * side, Vector3.up) * offset;
        candidate.y = origin.y;
        NavMeshQueryFilter filter = new NavMeshQueryFilter
        {
            agentTypeID = _agent.agentTypeID,
            areaMask = _agent.areaMask
        };
        //특정 좌표에서 일정 거리 이내에 존재하는 가장 가까운 NavMesh 지점 찾기
        if(!NavMesh.SamplePosition(candidate, out NavMeshHit sampledHit, sampleRadius, filter)) return false;
        if(Mathf.Abs(sampledHit.position.y - origin.y) > maxHeightDifference) return false;
        float minimumTravle = _agent.stoppingDistance * MinimumTravelMultiplier;
        if((sampledHit.position - origin).sqrMagnitude <= minimumTravle * minimumTravle) return false;
        //NavMesh 위에서 직접 이동 가능한지 확인
        if(_agent.Raycast(sampledHit.position, out _)) return false;
        if(!_agent.SetDestination(sampledHit.position)) return false;
        _agent.isStopped = false;
        _remainingMoveTime = Mathf.Max(MinimumInterval, moveTimeout);
        _moving = true;
        return true;
    }

    public void End()
    {
        if(!_running) return;
        if(_owner != null) _owner.StopMoving();
        if(_agent != null)
        {
            _agent.speed = _previousSpeed;
            _agent.stoppingDistance = _previousStoppingDistance;
            _agent.updateRotation = _previousUpdateRotation;
        }
        _moving = false;
        _running = false;
    }
    private float GetPauseDuration()
    {
        float minimum = Mathf.Max(MinimumInterval, minPauseDuration);
        float maximum = Mathf.Max(minimum, maxPauseDuration);

        return Random.Range(minimum, maximum);
    }

    private void OnDisable()
    {
        End();
    }
}

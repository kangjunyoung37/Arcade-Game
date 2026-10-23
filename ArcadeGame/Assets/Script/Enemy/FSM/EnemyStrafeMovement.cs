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
    private const float MinimumPauseDuration =0.02f;
    private const float RetreatLookAheadMultiplier = 2f;
    [Header("Movement")]
    [SerializeField, Min(0.1f)] private float moveSpeed = 4f;
    [SerializeField, Min(0.1f)] private float moveAcceleration = 60f;
    [SerializeField, Min(0.1f)] private float minStrafeDistance = 1.2f;
    [SerializeField, Min(0.1f)] private float maxStrafeDistance = 3.5f;
    [SerializeField, Min(0.1f)] private float stoppingDistance = 0.15f;
    [SerializeField, Min(0.1f)] private float moveTimeout = 2.5f;
    
    [Header("Pause Between Moves")]
    [SerializeField, Min(MinimumPauseDuration)] private float minPauseDuration = 0.05f;
    [SerializeField, Min(MinimumPauseDuration)] private float maxPauseDuration = 0.2f;
    [SerializeField, Min(MinimumInterval)] private float blockedRetryDelay = 0.3f;

    [Header("Retreat")]
    [SerializeField, Min(0.1f)] private float retreatStepDistance = 1.5f;
    [SerializeField, Range(0f, 60f)] private float retreatSideAngle = 35f;
    [SerializeField, Min(MinimumInterval)] private float retreatRepathInterval = 0.2f;

    [Header("NavMesh Validation")]
    [SerializeField, Min(0.05f)] private float sampleRadius = 0.4f;
    [SerializeField, Min(0f)] private float maxHeightDifference = 0.5f;
    
    [Header("Apprach")]
    [SerializeField, Min(MinimumInterval)] private float approachRepathInterval = 0.2f;
    
    private EnemyController _owner;
    private EnemyCombatMoveIntent _movementIntent;
    private NavMeshAgent _agent;
    private NavMeshPath _approachPath;
    private bool _running;
    private bool _moving;
    private int _side;
    
    private float _remainingApproachRepath;
    private float _remainingPause;
    private float _remainingMoveTime;

    private float _previousSpeed;
    private float _previousAcceleration;
    private float _previousStoppingDistance;
    private bool _previousUpdateRotation;
    private float _remainingRetreatRepath;
    private bool _previousAutoBraking;
    private bool CanNavigate => _agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh;
    public bool CanExecuteCombatMovement => _running && isActiveAndEnabled && CanNavigate;
    public bool ApproachDestinationReached{ get; private set;}
    private void Awake()
    {
        _owner = GetComponent<EnemyController>();
        _agent = GetComponent<NavMeshAgent>();
        _approachPath = new NavMeshPath();
    }
    public void Begin()
    {
        if(_running || !isActiveAndEnabled || _owner == null || !CanNavigate) return;
        _previousSpeed = _agent.speed;
        _previousAcceleration = _agent.acceleration;
        _previousStoppingDistance = _agent.stoppingDistance;
        _previousUpdateRotation = _agent.updateRotation;
        _previousAutoBraking = _agent.autoBraking;
        _remainingRetreatRepath = 0f;
        _remainingApproachRepath = 0f;
        ApproachDestinationReached = false;

        _owner.StopMoving();
        _agent.speed = Mathf.Max(MinimumInterval, moveSpeed);
        _agent.acceleration = Mathf.Max(MinimumInterval, moveAcceleration);
        _agent.stoppingDistance = Mathf.Max(0.05f, stoppingDistance);
        _agent.updateRotation = false;

        _side = Random.value < 0.5f ? -1 : 1;
        _remainingPause = GetPauseDuration();
        _moving = false;
        _movementIntent = EnemyCombatMoveIntent.Strafe;
        _running = true;
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
            _agent.acceleration = _previousAcceleration;
            _agent.autoBraking = _previousAutoBraking;
        }
        _moving = false;
        _running = false;
        _remainingApproachRepath = 0f;
        ApproachDestinationReached = false;
    }
    public void Tick(float deltaTime, EnemyCombatMoveIntent intent)
    {

        if(!_running || _owner == null || !CanNavigate || deltaTime <= 0f) return;
       
        if(_movementIntent != intent)
        {
            _movementIntent = intent;
            //이전 이동 목적지 취소 새 행동 전환
            _owner.StopMoving();

            _moving = false;
            _remainingPause = 0f;
            _remainingRetreatRepath = 0f;
            _remainingApproachRepath = 0f;
            ApproachDestinationReached = false;

            switch (intent)
            {
                case EnemyCombatMoveIntent.Retreat:
                    _agent.autoBraking = false;
                    break;
                
                case EnemyCombatMoveIntent.Approach:
                    _agent.autoBraking = true;
                    break;
                
                default:
                    _agent.autoBraking = _previousAutoBraking;
                    break;
            }
        }

        if(_movementIntent == EnemyCombatMoveIntent.Hold)
            return;

        if(_movementIntent == EnemyCombatMoveIntent.Retreat)
        {
            TickRetreat(deltaTime);
            return;
        }
        if(_movementIntent == EnemyCombatMoveIntent.Approach)
        {
            TickApproach(deltaTime);
            return;
        }
        if(_moving)
        {
            _remainingMoveTime -= deltaTime;
            bool pathFailed = !_agent.pathPending && (!_agent.hasPath || _agent.pathStatus != NavMeshPathStatus.PathComplete);
            bool arrived = !_agent.pathPending && !pathFailed && _agent.remainingDistance <= _agent.stoppingDistance;;
            bool timeOut = _remainingMoveTime <= 0f;

            if(arrived || pathFailed || timeOut)
            {
                _owner.StopMoving();
                _moving = false;
                _remainingPause = GetPauseDuration();

                if(!arrived && (pathFailed || timeOut))
                    ReportBlocked(EnemyCombatMoveIntent.Strafe);
            }
            return;
        }
        _remainingPause -= deltaTime;
        if(_remainingPause > 0f) return;
        
        _side = Random.value < 0.5f ? -1 : 1;

        if(TryStartMove(_side)) return;
        
        if (TryStartMove(-_side))
        {
            _side = -_side;
            return;
        }
        _remainingPause = Mathf.Max(MinimumInterval, blockedRetryDelay);
        ReportBlocked(EnemyCombatMoveIntent.Strafe);
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
        
        float configuredMinimum = Mathf.Max(0.1f,minStrafeDistance);
        float configuredMaximum = Mathf.Max(configuredMinimum, maxStrafeDistance);

        //플레이어에게 너무 가까우면 큰 각도로 돌아가지 않도록
        float maximumArcDistance = radius * MaximumSweepAngle * Mathf.Deg2Rad;
        
        //먼저 가능한 범위를 제한한 다음 그 안에서 추출
        float maximumDistance = Mathf.Min(configuredMaximum, maximumArcDistance);
        float minimumDistance = Mathf.Min(configuredMinimum, maximumDistance);
        float selectedDistance = Random.Range(minimumDistance,maximumDistance);
        float angle = selectedDistance /radius * Mathf.Rad2Deg;

        Vector3 candidate = center + Quaternion.AngleAxis(angle * side, Vector3.up) * offset;
        candidate.y = origin.y;
        return TryMoveToCandidate(origin,candidate,false);
    }
    private bool TryMoveToCandidate(Vector3 origin, Vector3 candidate, bool mustIncreaseDistance)
    {
        NavMeshQueryFilter filter = new NavMeshQueryFilter
        {
            agentTypeID = _agent.agentTypeID,
            areaMask = _agent.areaMask
        };
        if(!NavMesh.SamplePosition(candidate, out NavMeshHit sampledHit, sampleRadius, filter)) return false;
        if(Mathf.Abs(sampledHit.position.y - origin.y) > maxHeightDifference) return false;
        float minimumTravel = _agent.stoppingDistance * MinimumTravelMultiplier;
        if((sampledHit.position - origin).sqrMagnitude <= minimumTravel  * minimumTravel ) return false;
        if (mustIncreaseDistance)
        {
            Vector3 center = _owner.LastKnownPlayerPosition;
            
            Vector3 currentOffset = origin - center;
            Vector3 candidateOffset = sampledHit.position - center;

            currentOffset.y = 0f;
            candidateOffset.y = 0f;
            if(candidateOffset.sqrMagnitude <= currentOffset.sqrMagnitude + DirectionSqrEpsion) return false;
        }
        if(_agent.Raycast(sampledHit.position, out _)) return false;
        if(!_agent.SetDestination(sampledHit.position)) return false;
        _agent.isStopped = false;
        _remainingMoveTime = Mathf.Max(MinimumInterval, moveTimeout);
        _moving = true;
        return true;
    }

    private bool TryStartRetreat()
    {
        if(!_owner.HasTarget || !_owner.CanSeePlayer) return false;

        Vector3 orgin = _agent.nextPosition - Vector3.up * _agent.baseOffset;
        Vector3 away = orgin - _owner.LastKnownPlayerPosition;
        away.y = 0f;

        //겹쳤을 경우 반대로 이동
        if(away.sqrMagnitude <= DirectionSqrEpsion)
        {
            away = -transform.forward;
            away.y = 0f;
        }
        if(away.sqrMagnitude <= DirectionSqrEpsion) return false;

        away.Normalize();
        float interval = Mathf.Max(MinimumInterval, retreatRepathInterval);
        
        //다음 갱신 전에 목저지에 도착하지 않도록 이동 속도와 갠신 주기에 맞춰 여유 거리 확보
        float lookAheadDistance = _agent.speed * interval * RetreatLookAheadMultiplier + _agent.stoppingDistance;
        float distance = Mathf.Max(Mathf.Max(MinimumInterval, retreatStepDistance),lookAheadDistance);
        //정면 반대 방향으로 후퇴
        Vector3 candidate = orgin + away * distance;
        if(TryMoveToCandidate(orgin, candidate, true)) return true;
        //뒤가 막혀있을 경우 대각선을 채택
        float firstSide = Random.value < 0.5f ? -1f : 1f;

        Vector3 diagonal = Quaternion.AngleAxis(retreatSideAngle * firstSide, Vector3.up) * away;
        candidate = orgin + diagonal * distance;
        if(TryMoveToCandidate(orgin, candidate, true)) return true;
        //반대쪽 대각선 검사
        diagonal = Quaternion.AngleAxis(-retreatSideAngle * firstSide, Vector3.up) * away;
        candidate = orgin + diagonal * distance;
        return TryMoveToCandidate(orgin, candidate, true);
    }

    private void TickRetreat(float deltaTime)
    {
        _remainingRetreatRepath -= deltaTime;
        // 갱신 주기가 되지 않았거나 경로 계산 중이면 기존 목적지로 이동
        if(_remainingRetreatRepath > 0f || _agent.pathPending) return;

        _remainingRetreatRepath = Mathf.Max(MinimumInterval, retreatRepathInterval);

        // 이동을 멈추지 않고 다음 후퇴 목적지로 이동
        if(TryStartRetreat()) return;
        _owner.StopMoving();
        _moving = false;

        _remainingRetreatRepath = Mathf.Max(MinimumInterval, blockedRetryDelay);
    }
    private float GetPauseDuration()
    {
        float minimum = Mathf.Max(MinimumPauseDuration, minPauseDuration);
        float maximum = Mathf.Max(minimum, maxPauseDuration);

        return Random.Range(minimum, maximum);
    }
    
    private void TickApproach(float deltaTime)
    {
        EnemyCombatDecision decision = _owner.CombatDecision;

        if(!_owner.HasTarget || !_owner.CanSeePlayer || decision == null || !decision.isActiveAndEnabled)
        {
            StopApproachForRetry();
            return;
        }

        if(ApproachDestinationReached)
            return;
        
        _remainingApproachRepath -= deltaTime;

        if (_moving)
        {
            _remainingMoveTime -= deltaTime;
            if (_agent.pathPending)
            {
                if(_remainingMoveTime <= 0f)
                    StopApproachForRetry();
                
                return;
            }

            if(!_agent.hasPath || _agent.pathStatus != NavMeshPathStatus.PathComplete)
            {
                StopApproachForRetry();
                return;
            }

            if(_agent.remainingDistance <= _agent.stoppingDistance)
            {
                _owner.StopMoving();
                _moving = false;
                ApproachDestinationReached = true;
                return;
            }
        }
        if(_remainingApproachRepath > 0f)
            return;
        
        _remainingApproachRepath = Mathf.Max(MinimumInterval, approachRepathInterval);

        if(!TryStartApproach(decision.ApproachStopDistance))
            StopApproachForRetry();

    }

    private bool TryStartApproach(float desireDistance)
    {
        Vector3 origin = _agent.nextPosition - Vector3.up * _agent.baseOffset;
        Vector3 targetPosition = _owner.LastKnownPlayerPosition;
        Vector3 toTarget = targetPosition - origin;
        toTarget.y = 0f;
        if(toTarget.sqrMagnitude <= DirectionSqrEpsion)
            return false;
        float currentDistance = toTarget.magnitude;
        if(currentDistance <= desireDistance)
            return false;
        Vector3 direction = toTarget / currentDistance;

        // 플레이어 위치까지 가지 않고 교전 거리를 남긴 지점으로 이동
        Vector3 candidate = origin + direction * (currentDistance - desireDistance);
        NavMeshQueryFilter filter = new NavMeshQueryFilter
        {
            agentTypeID = _agent.agentTypeID,
            areaMask = _agent.areaMask
        };

        if(!NavMesh.SamplePosition(candidate, out NavMeshHit hit, sampleRadius, filter))
            return false;
        
        if(Mathf.Abs(hit.position.y - origin.y) > maxHeightDifference)
            return false;
        
        Vector3 candidateOffset = hit.position - targetPosition;
        candidateOffset.y = 0f;

         //실제로 가까워지는 목적지인지 확인
         if(candidateOffset.sqrMagnitude >= toTarget.sqrMagnitude)
            return false;

        float minimumTravel = _agent.stoppingDistance * MinimumTravelMultiplier;

        if((hit.position - origin).sqrMagnitude <= minimumTravel * minimumTravel)
            return false;
        
        if(_approachPath == null)
            return false;

        // 모든 경로 포함해서 계산
        if(!_agent.CalculatePath(hit.position, _approachPath))   
            return false;
        
        if(_approachPath.status != NavMeshPathStatus.PathComplete)
            return false;
       
        if(!_agent.SetPath(_approachPath))
            return false;
        
        _agent.isStopped = false;
        _moving = true;
        _remainingMoveTime = Mathf.Max(MinimumInterval, moveTimeout);

        return true;
    }

    private void StopApproachForRetry()
    {
        if(_moving)
            _owner.StopMoving();
        
        _moving = false;
        ApproachDestinationReached = false;
        
        _remainingApproachRepath = Mathf.Max(MinimumInterval, blockedRetryDelay);
        if(_owner != null && _owner.HasTarget && _owner.CanSeePlayer)
            ReportBlocked(EnemyCombatMoveIntent.Approach);

    }

    private void ReportBlocked(EnemyCombatMoveIntent intent)
    {
        if(_owner == null)
            return;
        EnemyCombatDecision decision = _owner.CombatDecision;

        if(decision != null && decision.isActiveAndEnabled)
            decision.ReportMovmentBlocked(intent);
    }
    private void OnDisable()
    {
        End();
    }
}

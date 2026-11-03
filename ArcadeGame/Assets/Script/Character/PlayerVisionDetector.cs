using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerVisionDetector : MonoBehaviour
{
    private const float MinimumScanInterval = 0.02f;
    private const float DirectionSqrEpsilon = 0.0001f;
    private const float HalfAngleMultiplier = 0.5f;
    private const float FullCircleAngle = 360f;

    [Header("Vision Sources")]
    [SerializeField] private FieldOfView forwardView;
    [SerializeField] private FieldOfView nearbyView;

    [Header("Detection")]
    [SerializeField] private Transform eyePoint;
    [SerializeField] private LayerMask targetMask;
    [SerializeField] private LayerMask obstacleMask;

    [SerializeField, Min(MinimumScanInterval)] private float scanInterval = 0.1f;
    
    [Header("Visibility Stability")]
    [Tooltip("이미 보이는 대상에게만 적용하는 추가 거리입니다.")]
    [SerializeField, Min(0f)] private float visibleDistanceMargin = 0.25f;

    [Tooltip("이미 보이는 대상에게만 적용하는 좌우 각각의 추가 각도입니다.")]
    [SerializeField, Min(0f)] private float visibleAngleMargin = 2f;
    private float _nextScanTime;
    private void Awake()
    {
        if (forwardView != null)
            return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.LogError("PlayerVisionDetector의 Forward View를 연결해 주세요.",this);
#endif
        enabled = false;
    }

    private void OnEnable()
    {
        _nextScanTime = 0f;
    }

    private void Update()
    {
        if (Time.time < _nextScanTime)
            return;
        _nextScanTime = Time.time + Mathf.Max(MinimumScanInterval, scanInterval);
        ScanVisibility();
    }

    private void OnDisable()
    {
        // 이 로컬 플레이어의 시야 관리가 중단되면 표시를 해제합니다.
        for (int i = 0; i < HideableEntity.ActiveCount; i++)
        {
            HideableEntity entity = HideableEntity.GetActiveEntity(i);
            if (entity != null)
                entity.SetVisibility(false);
        }
    }

    private void ScanVisibility()
    {
        bool forwardEnabled = forwardView != null && forwardView.isActiveAndEnabled && forwardView.viewRadius > 0f && forwardView.viewAngle > 0f;

        bool nearbyEnabled = nearbyView != null && nearbyView.isActiveAndEnabled && nearbyView.viewRadius > 0f;

        float forwardRadius = forwardEnabled ? forwardView.viewRadius : 0f;

        float nearbyRadius = nearbyEnabled ? nearbyView.viewRadius: 0f;

        float forwardRadiusSquared = forwardRadius * forwardRadius;
        float nearbyRadiusSquared = nearbyRadius * nearbyRadius;

        float distanceMargin = Mathf.Max(0f, visibleDistanceMargin);

        // 이미 보이는 대상에게만 사용하는 유지 거리
        float retainForwardRadius = forwardRadius + distanceMargin;
        float retainNearbyRadius = nearbyRadius + distanceMargin;

        float retainForwardRadiusSquared = retainForwardRadius * retainForwardRadius;
        float retainNearbyRadiusSquared = retainNearbyRadius * retainNearbyRadius;

        Vector3 forward = forwardEnabled ? forwardView.transform.forward : transform.forward;

        forward.y = 0f;

        bool validForward = forward.sqrMagnitude > DirectionSqrEpsilon;

        if (validForward)
            forward.Normalize();

        float angle = forwardEnabled ? Mathf.Clamp(forwardView.viewAngle, 0f, FullCircleAngle) : 0f;

        float halfAngle = angle * HalfAngleMultiplier;

        // 처음 발견할 때의 각도 기준입니다.
        float viewDotThreshold = Mathf.Cos(halfAngle * Mathf.Deg2Rad);

        // 좌우 각각에 여유 각도를 더합니다.
        float retainHalfAngle = Mathf.Min(FullCircleAngle * HalfAngleMultiplier, halfAngle + Mathf.Max(0f, visibleAngleMargin));

        float retainDotThreshold = Mathf.Cos(retainHalfAngle * Mathf.Deg2Rad);

        Vector3 rangeOrigin = transform.position;

        Vector3 rayOrigin = eyePoint != null ? eyePoint.position : rangeOrigin;

        int entityCount = HideableEntity.ActiveCount;
        for (int i = 0; i < entityCount; i++)
        {
            HideableEntity entity = HideableEntity.GetActiveEntity(i);

            if (entity == null)
                continue;

            bool layerAllowed = (targetMask.value & (1 << entity.gameObject.layer)) != 0;

            if (!layerAllowed)
            {
                entity.SetVisibility(false);
                continue;
            }

            // 이번 검사 직전의 표시 상태를 기준으로 선택합니다.
            bool wasVisible = entity.IsVisible;

            float selectedDotThreshold = wasVisible ? retainDotThreshold : viewDotThreshold;

            float selectedForwardRadiusSquared = wasVisible ? retainForwardRadiusSquared : forwardRadiusSquared;

            float selectedNearbyRadiusSquared = wasVisible ? retainNearbyRadiusSquared : nearbyRadiusSquared;

            bool visible = CanSeeEntity(
                entity,
                rangeOrigin,
                rayOrigin,
                forward,
                validForward,
                selectedDotThreshold,
                forwardEnabled,
                nearbyEnabled,
                selectedForwardRadiusSquared,
                selectedNearbyRadiusSquared);

            entity.SetVisibility(visible);
        }
}

    private bool CanSee(Vector3 targetPosition,Vector3 rangeOrigin,Vector3 rayOrigin,Vector3 forward,bool validForward,float viewDotThreshold,bool forwardEnabled,bool nearbyEnabled,float forwardRadiusSquared,float nearbyRadiusSquared)
    {
        Vector3 horizontalOffset = targetPosition - rangeOrigin;
        horizontalOffset.y = 0f;

        float distanceSquared = horizontalOffset.sqrMagnitude;
        bool insideNearby = nearbyEnabled && distanceSquared <= nearbyRadiusSquared;
        bool insideForward = false;

        if (forwardEnabled && validForward && distanceSquared <= forwardRadiusSquared)
        {
            if (distanceSquared <= DirectionSqrEpsilon)
            {
                insideForward = true;
            }
            else
            {
                float dot = Vector3.Dot(forward, horizontalOffset.normalized);
                insideForward = dot >= viewDotThreshold;
            }
        }

        if (!insideNearby && !insideForward)
            return false;

        // 근거리 원형 시야도 벽을 통과해서 보지는 못합니다.
        Vector3 rayDirection = targetPosition - rayOrigin;
        float rayDistance = rayDirection.magnitude;

        if (rayDistance <= Mathf.Epsilon)
            return true;

        return !Physics.Raycast(rayOrigin, rayDirection / rayDistance, rayDistance, obstacleMask, QueryTriggerInteraction.Ignore);
    }
    private bool CanSeeEntity(HideableEntity entity, Vector3 rangeOrigin, Vector3 rayOrigin, Vector3 forward, bool validForward, float viewDotThreshold, bool forwardEnabled, bool nearbyEnabled, float forwardRadiusSquared, float nearbyRadiusSquared)
    {
        if (entity == null)
            return false;

        int sampleCount = entity.VisibilitySampleCount;

        for (int i = 0; i < sampleCount; i++)
        {
            if (!entity.TryGetVisibilitySample(
                    i,
                    out Vector3 samplePosition))
            {
                continue;
            }

            bool sampleVisible = CanSee(
                samplePosition,
                rangeOrigin,
                rayOrigin,
                forward,
                validForward,
                viewDotThreshold,
                forwardEnabled,
                nearbyEnabled,
                forwardRadiusSquared,
                nearbyRadiusSquared);
                
            if (sampleVisible)
                return true;
        }

        return false;
    }
}
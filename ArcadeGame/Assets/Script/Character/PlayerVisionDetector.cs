using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerVisionDetector : MonoBehaviour
{
    private const float MinimumScanInterval = 0.02f;
    private const float DirectionSqrEpsilon = 0.0001f;
    private const float HalfAngleMultiplier = 0.5f;

    [Header("Vision Sources")]
    [SerializeField] private FieldOfView forwardView;
    [SerializeField] private FieldOfView nearbyView;

    [Header("Detection")]
    [SerializeField] private Transform eyePoint;
    [SerializeField] private LayerMask targetMask;
    [SerializeField] private LayerMask obstacleMask;

    [SerializeField, Min(MinimumScanInterval)] private float scanInterval = 0.1f;
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
        bool forwardEnabled = forwardView != null && forwardView.isActiveAndEnabled;

        bool nearbyEnabled = nearbyView != null && nearbyView.isActiveAndEnabled;

        float forwardRadius = forwardEnabled ? Mathf.Max(0f, forwardView.viewRadius) : 0f;

        float nearbyRadius = nearbyEnabled ? Mathf.Max(0f, nearbyView.viewRadius) : 0f;

        float forwardRadiusSquared = forwardRadius * forwardRadius;
        float nearbyRadiusSquared = nearbyRadius * nearbyRadius;

        Vector3 forward = forwardEnabled ? forwardView.transform.forward : transform.forward;
        forward.y = 0f;
        bool validForward = forward.sqrMagnitude > DirectionSqrEpsilon;

        if (validForward)
            forward.Normalize();

        float angle = forwardEnabled ? Mathf.Clamp(forwardView.viewAngle, 0f, 360f) : 0f;

        float viewDotThreshold = Mathf.Cos(angle * HalfAngleMultiplier * Mathf.Deg2Rad);

        Vector3 rangeOrigin = transform.position;
        Vector3 rayOrigin = eyePoint != null ? eyePoint.position : rangeOrigin;

        for (int i = 0; i < HideableEntity.ActiveCount; i++)
        {
            HideableEntity entity = HideableEntity.GetActiveEntity(i);

            if (entity == null)
                continue;
            bool layerAllowed = (targetMask.value & (1 << entity.gameObject.layer)) != 0;

            bool visible = layerAllowed && CanSee(
                entity.VisibilityPosition,
                rangeOrigin,
                rayOrigin,
                forward,
                validForward,
                viewDotThreshold,
                forwardEnabled,
                nearbyEnabled,
                forwardRadiusSquared,
                nearbyRadiusSquared);

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
}
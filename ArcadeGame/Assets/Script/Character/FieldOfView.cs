using UnityEngine;

[DisallowMultipleComponent]
public sealed class FieldOfView : MonoBehaviour
{
    private const int MinimumSegments = 4;
    private const int MaximumSegments = 512;
    private const int TriangleIndexCount = 3;

    private const float MinimumRadius = 0.01f;
    private const float FullCircleAngle = 360f;
    private const float Half = 0.5f;

    private const string RuntimeMeshName = "RuntimeVisionMesh";

    [Header("FOV Settings")]
    [Min(0f)] public float viewRadius = 10f;

    [Range(0f, FullCircleAngle)] public float viewAngle = 90f;

    public LayerMask obstacleMask;

    [Header("Mesh Settings")]
    public MeshFilter viewMeshFilter;

    [Header("Mask Projection")]
    [Tooltip("시야 마스크를 그릴 월드 공간의 바닥 높이입니다.")]
    [SerializeField] private float maskPlaneHeight = 0f;

    [SerializeField, Range(MinimumSegments, MaximumSegments)] private int raySegments = 96;

    private Transform _meshTransform;

    private Mesh _viewMesh;
    private Mesh _previousMesh;

    private Vector3[] _vertices;
    private Vector2[] _uvs;
    private Vector3[] _localRayDirections;
    private int[] _triangles;

    private int _cachedDirectionSegments = -1;
    private float _cachedDirectionAngle = -1f;
    private int _uploadedSegments = -1;

    private void Awake()
    {
        if (viewMeshFilter == null)
            TryGetComponent(out viewMeshFilter);

        if (viewMeshFilter == null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogError("FieldOfView의 View Mesh Filter를 연결해 주세요.", this);
#endif
            enabled = false;
            return;
        }

        _meshTransform = viewMeshFilter.transform;
        _previousMesh = viewMeshFilter.sharedMesh;

        // 최대 크기로 한 번 생성하고 필요한 범위만 사용합니다.
        _vertices = new Vector3[MaximumSegments + 2];
        _uvs = new Vector2[MaximumSegments + 2];
        _localRayDirections = new Vector3[MaximumSegments + 1];
        _triangles = new int[MaximumSegments * TriangleIndexCount];

        for (int i = 0; i < MaximumSegments; i++)
        {
            int index = i * TriangleIndexCount;

            _triangles[index] = 0;
            _triangles[index + 1] = i + 1;
            _triangles[index + 2] = i + 2;
        }

        _viewMesh = new Mesh
        {
            name = RuntimeMeshName
        };

        _viewMesh.MarkDynamic();
        viewMeshFilter.sharedMesh = _viewMesh;
    }

    private void LateUpdate()
    {
        if (_viewMesh == null || _meshTransform == null)
            return;

        float radius = Mathf.Max(0f, viewRadius);
        float angle = Mathf.Clamp(viewAngle, 0f, FullCircleAngle);

        if (radius < MinimumRadius || angle <= 0f)
        {
            ClearViewMesh();
            return;
        }

        int segments = Mathf.Clamp(
            raySegments,
            MinimumSegments,
            MaximumSegments);

        PrepareRayDirections(segments, angle);
        UpdateViewMesh(segments, radius);
    }

    private void PrepareRayDirections(int segments, float angle)
    {
        if (_cachedDirectionSegments == segments &&
            Mathf.Approximately(_cachedDirectionAngle, angle))
        {
            return;
        }

        float startAngle = -angle * Half;
        float angleStep = angle / segments;

        for (int i = 0; i <= segments; i++)
        {
            float radians =
                (startAngle + angleStep * i) * Mathf.Deg2Rad;

            _localRayDirections[i] = new Vector3(
                Mathf.Sin(radians),
                0f,
                Mathf.Cos(radians));
        }

        if (angle >= FullCircleAngle)
        {
            _localRayDirections[segments] =
                _localRayDirections[0];
        }

        _cachedDirectionSegments = segments;
        _cachedDirectionAngle = angle;
    }

    private void UpdateViewMesh(int segments, float radius)
    {
        int vertexCount = segments + 2;
        int indexCount = segments * TriangleIndexCount;

        Vector3 origin = transform.position;

        Quaternion yawRotation = Quaternion.Euler( 0f, transform.eulerAngles.y,0f);

        Matrix4x4 worldToMesh = _meshTransform.worldToLocalMatrix;

        float inverseDiameter = Half / radius;

        Vector3 meshOrigin = origin;
        meshOrigin.y = maskPlaneHeight;

        _vertices[0] = worldToMesh.MultiplyPoint3x4(meshOrigin);
        _uvs[0] = new Vector2(Half, Half);

        for (int i = 0; i <= segments; i++)
        {
            Vector3 localDirection = _localRayDirections[i];
            Vector3 worldDirection = yawRotation * localDirection;

            float distance = radius;

            if (Physics.Raycast(
                origin,
                worldDirection,
                out RaycastHit hit,
                radius,
                obstacleMask,
                QueryTriggerInteraction.Ignore))
            {
                distance = hit.distance;
            }

            Vector3 worldPoint = origin + worldDirection * distance;
            worldPoint.y = maskPlaneHeight;

            int vertexIndex = i + 1;

            _vertices[vertexIndex] = worldToMesh.MultiplyPoint3x4(worldPoint);

            // 장애물에 잘려도 전체 시야 반경 기준 UV를 유지
            Vector3 localOffset = localDirection * distance;

            _uvs[vertexIndex] = new Vector2( Half + localOffset.x * inverseDiameter, Half + localOffset.z * inverseDiameter);
        }

        bool topologyChanged = _uploadedSegments != segments;

        if (topologyChanged)
            _viewMesh.Clear();

        _viewMesh.SetVertices(_vertices, 0, vertexCount);
        _viewMesh.SetUVs(0, _uvs, 0, vertexCount);

        if (topologyChanged)
        {
            _viewMesh.SetTriangles( _triangles, 0, indexCount, 0,false, 0);
            _uploadedSegments = segments;
        }

        // 장애물에 따라 Mesh 영역이 바뀌므로 컬링 범위를 갱신
        _viewMesh.RecalculateBounds();
    }

    private void ClearViewMesh()
    {
        if (_viewMesh == null || _uploadedSegments < 0)
            return;

        _viewMesh.Clear();
        _uploadedSegments = -1;
    }

    private void OnDisable()
    {
        ClearViewMesh();
    }

    private void OnDestroy()
    {
        if (_viewMesh == null)
            return;

        if (viewMeshFilter != null &&
            viewMeshFilter.sharedMesh == _viewMesh)
        {
            viewMeshFilter.sharedMesh = _previousMesh;
        }

        Destroy(_viewMesh);
        _viewMesh = null;
    }
}